-- 添加更多学生信息供注册测试
INSERT INTO students (student_id, name, department) VALUES ('STU004', '赵六', '计算机学院');
INSERT INTO students (student_id, name, department) VALUES ('STU005', '孙七', '电子信息学院');
INSERT INTO students (student_id, name, department) VALUES ('STU006', '周八', '机械工程学院');
INSERT INTO students (student_id, name, department) VALUES ('STU007', '吴九', '外语学院');
INSERT INTO students (student_id, name, department) VALUES ('STU008', '郑十', '经济管理学院');
INSERT INTO students (student_id, name, department) VALUES ('STU009', '冯十一', '物理学院');
INSERT INTO students (student_id, name, department) VALUES ('STU010', '陈十二', '化学学院');
COMMIT;

-- 查看所有学生信息
SELECT * FROM students ORDER BY student_id; 