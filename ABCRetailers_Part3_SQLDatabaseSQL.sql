--Create User Table
CREATE TABLE Users (
	Id INT PRIMARY KEY IDENTITY(1,1),
	Username NVARCHAR(100) NOT NULL,
	PasswordHash NVARCHAR(256) NOT NULL,
	Role NVARCHAR(20) NOT NULL -- 'Customer' or 'Admin'
	);

-- User Login Data
INSERT INTO Users (Username, PasswordHash, Role)
VALUES
	('JonhDoe', 'PasswordJohn', 'Customer'),
	('Admin0001', 'AdminPassword1', 'Admin');


-- Show Data from User Table
SELECT * FROM Users;

-- Create Cart Table for shopping cart data
CREATE TABLE Cart (
	Id INT PRIMARY KEY IDENTITY,
	CustomerUsername NVARCHAR(100),
	ProductId NVARCHAR(100),
	Quantity INT
	);


-- Show Data from Cart Table
SELECT * FROM Cart;
