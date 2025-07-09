-- 禁用所有外键约束
BEGIN
    FOR r IN (
        SELECT 'ALTER TABLE "' || table_name || '" DISABLE CONSTRAINT "' || constraint_name || '"' AS stmt
        FROM user_constraints
        WHERE constraint_type IN ('R')
    ) LOOP
        EXECUTE IMMEDIATE r.stmt;
    END LOOP;
END;
/

-- 按依赖顺序删除表
DROP TABLE report_evidence CASCADE CONSTRAINTS;
DROP TABLE report CASCADE CONSTRAINTS;
DROP TABLE review CASCADE CONSTRAINTS;
DROP TABLE notification CASCADE CONSTRAINTS;
DROP TABLE exchange_requests CASCADE CONSTRAINTS;
DROP TABLE audit_logs CASCADE CONSTRAINTS;
DROP TABLE admins CASCADE CONSTRAINTS;
DROP TABLE negotiations CASCADE CONSTRAINTS;
DROP TABLE recharge_records CASCADE CONSTRAINTS;
DROP TABLE virtual_accounts CASCADE CONSTRAINTS;
DROP TABLE orders CASCADE CONSTRAINTS;
DROP TABLE abstract_orders CASCADE CONSTRAINTS;
DROP TABLE product_images CASCADE CONSTRAINTS;
DROP TABLE products CASCADE CONSTRAINTS;
DROP TABLE categories CASCADE CONSTRAINTS;
DROP TABLE email_verification CASCADE CONSTRAINTS;
DROP TABLE login_logs CASCADE CONSTRAINTS;
DROP TABLE credit_history CASCADE CONSTRAINTS;
DROP TABLE users CASCADE CONSTRAINTS;
DROP TABLE students CASCADE CONSTRAINTS;

-- 启用所有外键约束（如需恢复）
BEGIN
    FOR r IN (
        SELECT 'ALTER TABLE "' || table_name || '" ENABLE CONSTRAINT "' || constraint_name || '"' AS stmt
        FROM user_constraints
        WHERE constraint_type IN ('R') AND status = 'DISABLED'
    ) LOOP
        EXECUTE IMMEDIATE r.stmt;
    END LOOP;
END;
/

-- 学生表（非自增主键保持不变）
CREATE TABLE students (
    student_id VARCHAR2(20) PRIMARY KEY,
    name VARCHAR2(50) NOT NULL,
    department VARCHAR2(50)
);

-- 用户表
CREATE TABLE users (
    user_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email VARCHAR2(100) UNIQUE NOT NULL,
    credit_score NUMBER(3,1) DEFAULT 60.0,
    password_hash VARCHAR2(128) NOT NULL,
    student_id VARCHAR2(20) UNIQUE NOT NULL REFERENCES students(student_id)
);

-- 信用历史表
CREATE TABLE credit_history (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL REFERENCES users(user_id),
    change_type VARCHAR2(20) CHECK (change_type IN ('交易完成','举报处罚','好评奖励')),
    new_score NUMBER(3,1) NOT NULL
);

-- 登录日志表
CREATE TABLE login_logs (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL REFERENCES users(user_id),
    ip_address VARCHAR2(45) NOT NULL,
    log_time TIMESTAMP DEFAULT SYSTIMESTAMP,
    device_type VARCHAR2(10) CHECK (device_type IN ('PC','Mobile')),
    risk_level NUMBER(1) CHECK (risk_level IN (0,1,2))
);

-- 邮箱验证表
CREATE TABLE email_verification (
    verification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL REFERENCES users(user_id),
    email VARCHAR2(100) NOT NULL,
    verification_code VARCHAR2(6) NOT NULL,
    token VARCHAR2(64) NOT NULL,
    expire_time TIMESTAMP NOT NULL,
    is_used NUMBER(1) DEFAULT 0 CHECK (is_used IN (0,1))
);

-- 分类表（自关联）
CREATE TABLE categories (
    category_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parent_id NUMBER REFERENCES categories(category_id),
    name VARCHAR2(50) NOT NULL
);

-- 商品表
CREATE TABLE products (
    product_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL REFERENCES users(user_id),
    category_id NUMBER NOT NULL REFERENCES categories(category_id),
    title VARCHAR2(100) NOT NULL,
    description CLOB,
    base_price NUMBER(10,2) NOT NULL,
    publish_time TIMESTAMP DEFAULT SYSTIMESTAMP,
    view_count NUMBER DEFAULT 0,
    auto_remove_time TIMESTAMP,
    status VARCHAR2(20) CHECK (status IN ('在售','已下架','交易中'))
);

-- 商品图片表
CREATE TABLE product_images (
    image_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    product_id NUMBER NOT NULL REFERENCES products(product_id),
    image_url VARCHAR2(200) NOT NULL
);

-- 抽象订单表
CREATE TABLE abstract_orders (
    abstract_order_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_type VARCHAR2(20) CHECK (order_type IN ('normal','exchange'))
);

-- 订单表
CREATE TABLE orders (
    order_id NUMBER PRIMARY KEY,
    buyer_id NUMBER NOT NULL REFERENCES users(user_id),
    seller_id NUMBER NOT NULL REFERENCES users(user_id),
    total_amount NUMBER NOT NULL,
    status VARCHAR2(20) CHECK (status IN ('待付款','已付款','已发货','已送达','已完成','已取消')),
    create_time TIMESTAMP DEFAULT SYSTIMESTAMP,
    expire_time TIMESTAMP,
    final_price NUMBER(10,2),
    CONSTRAINT fk_orders_abstract 
      FOREIGN KEY (order_id) REFERENCES abstract_orders(abstract_order_id)
);

-- 虚拟账户表
CREATE TABLE virtual_accounts (
    account_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER UNIQUE NOT NULL REFERENCES users(user_id),
    balance NUMBER(10,2) DEFAULT 0.00
);

-- 充值记录表
CREATE TABLE recharge_records (
    recharge_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL REFERENCES users(user_id),
    amount NUMBER(10,2) NOT NULL,
    status VARCHAR2(10) CHECK (status IN ('处理中','成功','失败')),
    create_time TIMESTAMP DEFAULT SYSTIMESTAMP,
    complete_time TIMESTAMP
);

-- 议价表
CREATE TABLE negotiations (
    negotiation_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id NUMBER NOT NULL REFERENCES orders(order_id),
    proposal_price NUMBER(10,2) NOT NULL,
    status VARCHAR2(20) CHECK (status IN ('待回应','已接受','已拒绝','反报价'))
);

-- 管理员表
CREATE TABLE admins (
    admin_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id NUMBER NOT NULL REFERENCES users(user_id),
    role VARCHAR2(20) CHECK (role IN ('super','category_admin','report_admin')),
    assigned_category NUMBER REFERENCES categories(category_id)
);

-- 审计日志表
CREATE TABLE audit_logs (
    log_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_id NUMBER NOT NULL REFERENCES admins(admin_id),
    action_type VARCHAR2(20) CHECK (action_type IN ('封禁用户','修改权限','处理举报')),
    target_id NUMBER NOT NULL,
    log_detail CLOB,
    log_time TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- 换物请求表
CREATE TABLE exchange_requests (
    exchange_id  NUMBER PRIMARY KEY,
    offer_product_id NUMBER NOT NULL REFERENCES products(product_id),
    request_product_id NUMBER NOT NULL REFERENCES products(product_id),
    terms CLOB,
    status VARCHAR2(20) CHECK (status IN ('待回应','接受','拒绝','反报价')),
    CONSTRAINT fk_exchange_abstract  -- 修改约束名
      FOREIGN KEY (exchange_id) REFERENCES abstract_orders(abstract_order_id)
);

-- 通知表
CREATE TABLE notification (
    notification_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    abstract_order_id NUMBER NOT NULL REFERENCES abstract_orders(abstract_order_id),
    content CLOB,
    send_status VARCHAR2(10) CHECK (send_status IN ('待发送','成功','失败')),
    retry_count NUMBER DEFAULT 0,
    last_attempt_time TIMESTAMP
);

-- 评价表
CREATE TABLE review (
    review_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    abstract_order_id NUMBER NOT NULL REFERENCES abstract_orders(abstract_order_id),
    rating NUMBER(1),
    desc_accuracy NUMBER(1),
    service_attitude NUMBER(1),
    is_anonymous NUMBER(1) DEFAULT 0,
    create_time TIMESTAMP DEFAULT SYSTIMESTAMP,
    seller_reply CLOB
);

-- 举报表
CREATE TABLE report (
    report_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    abstract_order_id NUMBER NOT NULL REFERENCES abstract_orders(abstract_order_id),
    type VARCHAR2(20) CHECK (type IN ('商品问题','服务问题','欺诈')),
    priority NUMBER(2)
);

-- 举报证据表
CREATE TABLE report_evidence (
    evidence_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    report_id NUMBER NOT NULL REFERENCES report(report_id),
    file_type VARCHAR2(10) CHECK (file_type IN ('image','video')),
    file_url VARCHAR2(200)
);

-- 创建索引（保持不变）
CREATE INDEX idx_orders_buyer ON orders(buyer_id);
CREATE INDEX idx_orders_seller ON orders(seller_id);
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_report_order ON report(abstract_order_id);