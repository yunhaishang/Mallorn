-- ================================================================
-- 校园交易平台数据库初始化脚本 (Oracle)
-- 根据数据库设计文档创建完整的数据库结构
-- ================================================================

-- 设置容器到 XEPDB1
ALTER SESSION SET CONTAINER=XEPDB1;

-- 创建用户和授权
ALTER SESSION SET "_ORACLE_SCRIPT"=true;

-- 创建表空间
CREATE TABLESPACE CAMPUS_TRADE_DATA
DATAFILE '/opt/oracle/oradata/XE/XEPDB1/campus_trade_data.dbf' SIZE 200M
AUTOEXTEND ON NEXT 20M MAXSIZE 2G;

-- 创建用户
CREATE USER CAMPUS_TRADE_USER IDENTIFIED BY "CampusTrade123!"
DEFAULT TABLESPACE CAMPUS_TRADE_DATA
TEMPORARY TABLESPACE TEMP
QUOTA UNLIMITED ON CAMPUS_TRADE_DATA;

-- 授权
GRANT CONNECT, RESOURCE, DBA TO CAMPUS_TRADE_USER;
GRANT CREATE SESSION TO CAMPUS_TRADE_USER;
GRANT CREATE TABLE TO CAMPUS_TRADE_USER;
GRANT CREATE SEQUENCE TO CAMPUS_TRADE_USER;
GRANT CREATE TRIGGER TO CAMPUS_TRADE_USER;

-- 连接到用户
CONNECT CAMPUS_TRADE_USER/CampusTrade123!@XEPDB1;

-- ================================================================
-- 创建序列 (用于自增ID)
-- ================================================================
CREATE SEQUENCE USER_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE PRODUCT_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE ORDER_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE CATEGORY_SEQ START WITH 1 INCREMENT BY 1 NOCACHE;

-- ================================================================
-- 1. 学生信息表 (students)
-- ================================================================
CREATE TABLE students (
    student_id VARCHAR2(20) PRIMARY KEY,
    name VARCHAR2(50) NOT NULL,
    department VARCHAR2(50)
);

-- ================================================================
-- 2. 用户表 (users)
-- ================================================================
CREATE TABLE users (
    user_id NUMBER PRIMARY KEY,
    email VARCHAR2(100) NOT NULL UNIQUE,
    credit_score NUMBER(3,1) DEFAULT 60.0,
    password_hash VARCHAR2(128) NOT NULL,
    student_id VARCHAR2(20) NOT NULL UNIQUE,
    username VARCHAR2(50),
    full_name VARCHAR2(100),
    phone VARCHAR2(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active NUMBER(1) DEFAULT 1 CHECK (is_active IN (0,1)),
    CONSTRAINT fk_users_student FOREIGN KEY (student_id) REFERENCES students(student_id)
);

-- ================================================================
-- 3. 信用变更记录表 (credit_history)
-- ================================================================
CREATE TABLE credit_history (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    change_type VARCHAR2(20) CHECK (change_type IN ('交易完成','举报处罚','好评奖励')),
    new_score NUMBER(3,1) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_credit_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 4. 登录日志表 (login_logs)
-- ================================================================
CREATE TABLE login_logs (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    ip_address VARCHAR2(45),
    log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    device_type VARCHAR2(20) CHECK (device_type IN ('Mobile','PC','Tablet')),
    risk_level NUMBER CHECK (risk_level IN (0,1,2)),
    CONSTRAINT fk_login_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 5. 邮箱验证表 (email_verification)
-- ================================================================
CREATE TABLE email_verification (
    verification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    email VARCHAR2(100) NOT NULL,
    verification_code VARCHAR2(6),
    token VARCHAR2(64),
    expire_time TIMESTAMP,
    is_used NUMBER DEFAULT 0 CHECK (is_used IN (0,1)),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_verification_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 6. 分类表 (categories)
-- ================================================================
CREATE TABLE categories (
    category_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parent_id NUMBER,
    name VARCHAR2(50) NOT NULL,
    CONSTRAINT fk_category_parent FOREIGN KEY (parent_id) REFERENCES categories(category_id)
);

-- ================================================================
-- 7. 商品表 (products)
-- ================================================================
CREATE TABLE products (
    product_id NUMBER PRIMARY KEY,
    user_id NUMBER NOT NULL,
    category_id NUMBER NOT NULL,
    title VARCHAR2(100) NOT NULL,
    description CLOB,
    base_price NUMBER(10,2) NOT NULL,
    publish_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    view_count NUMBER DEFAULT 0,
    auto_remove_time TIMESTAMP,
    status VARCHAR2(20) DEFAULT '在售' CHECK (status IN ('在售','已下架','交易中')),
    CONSTRAINT fk_product_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT fk_product_category FOREIGN KEY (category_id) REFERENCES categories(category_id)
);

-- ================================================================
-- 8. 商品图片表 (product_images)
-- ================================================================
CREATE TABLE product_images (
    image_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    product_id NUMBER NOT NULL,
    image_url VARCHAR2(200) NOT NULL,
    CONSTRAINT fk_image_product FOREIGN KEY (product_id) REFERENCES products(product_id)
);

-- ================================================================
-- 9. 抽象订单表 (abstract_orders)
-- ================================================================
CREATE TABLE abstract_orders (
    abstract_order_id NUMBER PRIMARY KEY,
    order_type VARCHAR2(20) CHECK (order_type IN ('normal','exchange'))
);

-- ================================================================
-- 10. 订单表 (orders)
-- ================================================================
CREATE TABLE orders (
    order_id NUMBER PRIMARY KEY,
    buyer_id NUMBER NOT NULL,
    seller_id NUMBER NOT NULL,
    product_id NUMBER NOT NULL,
    total_amount NUMBER(10,2),
    status VARCHAR2(20) DEFAULT '待付款' CHECK (status IN ('待付款','已付款','已发货','已送达','已完成','已取消')),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expire_time TIMESTAMP,
    final_price NUMBER(10,2),
    CONSTRAINT fk_order_abstract FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id),
    CONSTRAINT fk_order_buyer FOREIGN KEY (buyer_id) REFERENCES users(user_id),
    CONSTRAINT fk_order_seller FOREIGN KEY (seller_id) REFERENCES users(user_id),
    CONSTRAINT fk_order_product FOREIGN KEY (product_id) REFERENCES products(product_id)
);

-- ================================================================
-- 11. 虚拟账户表 (virtual_accounts)
-- ================================================================
CREATE TABLE virtual_accounts (
    account_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL UNIQUE,
    balance NUMBER(10,2) DEFAULT 0.00,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_account_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 12. 充值记录表 (recharge_records)
-- ================================================================
CREATE TABLE recharge_records (
    recharge_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL,
    amount NUMBER(10,2) NOT NULL,
    status VARCHAR2(20) DEFAULT '处理中' CHECK (status IN ('处理中','成功','失败')),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    complete_time TIMESTAMP,
    CONSTRAINT fk_recharge_user FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- ================================================================
-- 13. 议价表 (negotiations)
-- ================================================================
CREATE TABLE negotiations (
    negotiation_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    proposed_price NUMBER(10,2) NOT NULL,
    status VARCHAR2(20) DEFAULT '等待回应' CHECK (status IN ('等待回应','接受','拒绝','反报价')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_negotiation_order FOREIGN KEY (order_id) REFERENCES orders(order_id)
);

-- ================================================================
-- 14. 管理员表 (admins)
-- ================================================================
CREATE TABLE admins (
    admin_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL UNIQUE,
    role VARCHAR2(20) CHECK (role IN ('super','category_admin','report_admin')),
    assigned_category NUMBER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_admin_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT fk_admin_category FOREIGN KEY (assigned_category) REFERENCES categories(category_id),
    CONSTRAINT chk_admin_category CHECK (
        (role = 'category_admin' AND assigned_category IS NOT NULL) OR 
        (role != 'category_admin' AND assigned_category IS NULL)
    )
);

-- ================================================================
-- 15. 审计日志表 (audit_logs)
-- ================================================================
CREATE TABLE audit_logs (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_id NUMBER NOT NULL,
    action_type VARCHAR2(20) CHECK (action_type IN ('封禁用户','修改权限','处理举报')),
    target_id NUMBER,
    log_detail CLOB,
    log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_audit_admin FOREIGN KEY (admin_id) REFERENCES admins(admin_id)
);

-- ================================================================
-- 16. 换物请求表 (exchange_requests)
-- ================================================================
CREATE TABLE exchange_requests (
    exchange_id NUMBER PRIMARY KEY,
    offer_product_id NUMBER NOT NULL,
    request_product_id NUMBER NOT NULL,
    terms CLOB,
    status VARCHAR2(20) DEFAULT '等待回应' CHECK (status IN ('等待回应','接受','拒绝','反报价')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_exchange_abstract FOREIGN KEY (exchange_id) REFERENCES abstract_orders(abstract_order_id),
    CONSTRAINT fk_exchange_offer FOREIGN KEY (offer_product_id) REFERENCES products(product_id),
    CONSTRAINT fk_exchange_request FOREIGN KEY (request_product_id) REFERENCES products(product_id)
);

-- ================================================================
-- 17. 通知表 (notifications)
-- ================================================================
CREATE TABLE notifications (
    notification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    content CLOB NOT NULL,
    send_status VARCHAR2(20) DEFAULT '待发送' CHECK (send_status IN ('待发送','成功','失败')),
    retry_count NUMBER DEFAULT 0,
    last_attempt_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_notification_order FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id)
);

-- ================================================================
-- 18. 评价表 (reviews)
-- ================================================================
CREATE TABLE reviews (
    review_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    rating NUMBER(2,1) CHECK (rating BETWEEN 1 AND 5),
    desc_accuracy NUMBER(2,0) CHECK (desc_accuracy BETWEEN 1 AND 5),
    service_attitude NUMBER(2,0) CHECK (service_attitude BETWEEN 1 AND 5),
    is_anonymous NUMBER DEFAULT 0 CHECK (is_anonymous IN (0,1)),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    seller_reply CLOB,
    content CLOB,
    CONSTRAINT fk_review_order FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id)
);

-- ================================================================
-- 19. 举报表 (reports)
-- ================================================================
CREATE TABLE reports (
    report_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL,
    reporter_id NUMBER NOT NULL,
    type VARCHAR2(50) CHECK (type IN ('商品问题','服务问题','欺诈','虚假描述','其他')),
    priority NUMBER(2,0) CHECK (priority BETWEEN 1 AND 10),
    description CLOB,
    status VARCHAR2(20) DEFAULT '待处理' CHECK (status IN ('待处理','处理中','已处理','已关闭')),
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_report_order FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id),
    CONSTRAINT fk_report_user FOREIGN KEY (reporter_id) REFERENCES users(user_id)
);

-- ================================================================
-- 20. 举报证据表 (report_evidence)
-- ================================================================
CREATE TABLE report_evidence (
    evidence_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    report_id NUMBER NOT NULL,
    file_type VARCHAR2(20) CHECK (file_type IN ('图片','视频','文档')),
    file_url VARCHAR2(200) NOT NULL,
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_evidence_report FOREIGN KEY (report_id) REFERENCES reports(report_id)
);

-- ================================================================
-- 创建触发器
-- ================================================================

-- 用户ID自增触发器
CREATE OR REPLACE TRIGGER trg_users_id
    BEFORE INSERT ON users
    FOR EACH ROW
BEGIN
    IF :NEW.user_id IS NULL THEN
        :NEW.user_id := USER_SEQ.NEXTVAL;
    END IF;
END;
/

-- 用户更新时间触发器
CREATE OR REPLACE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
BEGIN
    :NEW.updated_at := CURRENT_TIMESTAMP;
END;
/

-- 商品ID自增触发器
CREATE OR REPLACE TRIGGER trg_products_id
    BEFORE INSERT ON products
    FOR EACH ROW
BEGIN
    IF :NEW.product_id IS NULL THEN
        :NEW.product_id := PRODUCT_SEQ.NEXTVAL;
    END IF;
END;
/

-- 订单过期时间触发器（默认30分钟）
CREATE OR REPLACE TRIGGER trg_orders_expire_time
    BEFORE INSERT ON orders
    FOR EACH ROW
BEGIN
    IF :NEW.expire_time IS NULL THEN
        :NEW.expire_time := CURRENT_TIMESTAMP + INTERVAL '30' MINUTE;
    END IF;
END;
/

-- 抽象订单和具体订单关联触发器
CREATE OR REPLACE TRIGGER trg_orders_abstract
    BEFORE INSERT ON orders
    FOR EACH ROW
BEGIN
    -- 先插入抽象订单
    INSERT INTO abstract_orders (abstract_order_id, order_type) 
    VALUES (:NEW.order_id, 'normal');
END;
/

-- 换物请求抽象订单触发器
CREATE OR REPLACE TRIGGER trg_exchange_abstract
    BEFORE INSERT ON exchange_requests
    FOR EACH ROW
BEGIN
    -- 先插入抽象订单
    INSERT INTO abstract_orders (abstract_order_id, order_type) 
    VALUES (:NEW.exchange_id, 'exchange');
END;
/

-- 用户注册时自动创建虚拟账户
CREATE OR REPLACE TRIGGER trg_users_virtual_account
    AFTER INSERT ON users
    FOR EACH ROW
BEGIN
    INSERT INTO virtual_accounts (user_id, balance)
    VALUES (:NEW.user_id, 0.00);
END;
/

-- ================================================================
-- 插入基础数据
-- ================================================================

-- 插入学生信息
INSERT INTO students (student_id, name, department) VALUES ('ADMIN001', '系统管理员', '计算机学院');
INSERT INTO students (student_id, name, department) VALUES ('STU001', '张三', '计算机学院');
INSERT INTO students (student_id, name, department) VALUES ('STU002', '李四', '电子信息学院');
INSERT INTO students (student_id, name, department) VALUES ('STU003', '王五', '机械工程学院');

-- 插入分类数据
-- 第一级分类
INSERT INTO categories (parent_id, name) VALUES (NULL, '教材');
INSERT INTO categories (parent_id, name) VALUES (NULL, '数码');
INSERT INTO categories (parent_id, name) VALUES (NULL, '日用');

-- 第二级分类 (教材)
INSERT INTO categories (parent_id, name) VALUES (1, '计算机类');
INSERT INTO categories (parent_id, name) VALUES (1, '数学类');
INSERT INTO categories (parent_id, name) VALUES (1, '英语类');

-- 第二级分类 (数码)
INSERT INTO categories (parent_id, name) VALUES (2, '手机');
INSERT INTO categories (parent_id, name) VALUES (2, '电脑');
INSERT INTO categories (parent_id, name) VALUES (2, '配件');

-- 第二级分类 (日用)
INSERT INTO categories (parent_id, name) VALUES (3, '文具');
INSERT INTO categories (parent_id, name) VALUES (3, '生活用品');
INSERT INTO categories (parent_id, name) VALUES (3, '服装');

-- 第三级分类 (计算机类教材)
INSERT INTO categories (parent_id, name) VALUES (4, '急出');
INSERT INTO categories (parent_id, name) VALUES (4, '可议价');
INSERT INTO categories (parent_id, name) VALUES (4, '支持换物');

-- 插入用户数据
-- 密码都是 "password" 的 BCrypt 哈希值: $2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi
INSERT INTO users (username, email, password_hash, full_name, student_id, credit_score) 
VALUES ('admin', 'admin@campus.edu', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', '系统管理员', 'ADMIN001', 100.0);

INSERT INTO users (username, email, password_hash, full_name, student_id, credit_score) 
VALUES ('zhangsan', 'zhangsan@campus.edu', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', '张三', 'STU001', 85.0);

INSERT INTO users (username, email, password_hash, full_name, student_id, credit_score) 
VALUES ('lisi', 'lisi@campus.edu', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', '李四', 'STU002', 75.0);

INSERT INTO users (username, email, password_hash, full_name, student_id, credit_score) 
VALUES ('wangwu', 'wangwu@campus.edu', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', '王五', 'STU003', 90.0);

-- 设置管理员
INSERT INTO admins (user_id, role) VALUES (1, 'super');

-- 插入测试商品数据
INSERT INTO products (user_id, category_id, title, description, base_price, status)
VALUES (2, 13, '数据结构与算法教材', '计算机专业必修课教材，九成新，无涂写', 45.00, '在售');

INSERT INTO products (user_id, category_id, title, description, base_price, status)
VALUES (3, 14, '高等数学习题册', '配套练习册，有少量笔记，可议价', 25.00, '在售');

INSERT INTO products (user_id, category_id, title, description, base_price, status)
VALUES (4, 15, '英语四级真题', '最新版本，支持换其他考试资料', 30.00, '在售');

-- 提交所有更改
COMMIT;

-- ================================================================
-- 验证表创建结果
-- ================================================================
SELECT 'Tables created successfully:' AS message FROM dual;
SELECT table_name FROM user_tables ORDER BY table_name;

SELECT 'Sample data verification:' AS message FROM dual;
SELECT COUNT(*) AS student_count FROM students;
SELECT COUNT(*) AS user_count FROM users;
SELECT COUNT(*) AS category_count FROM categories;
SELECT COUNT(*) AS product_count FROM products;
SELECT COUNT(*) AS virtual_account_count FROM virtual_accounts;

-- 显示用户和对应的虚拟账户
SELECT u.username, u.full_name, u.credit_score, va.balance 
FROM users u 
LEFT JOIN virtual_accounts va ON u.user_id = va.user_id; 