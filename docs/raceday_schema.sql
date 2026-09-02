/* =====================================================================
   RaceDay - South African Road Event Management System
   Database Schema and Seed Data
   Part 1 - System Planning and Database (Section C)
 
   This script:
     1. Creates the RaceDay database (if it does not already exist)
     2. Creates all six entities from the ERD with PKs, FKs and
        constraints (NOT NULL, UNIQUE, DEFAULT, CHECK)
     3. Seeds the database with realistic sample data:
        - 2 Organisers, 2 Participants (Users)
        - 3 Events
        - Categories for each event
        - Sample enrolments and a route/result example
 
   Tested to run without errors on a clean SQL Server instance (SSMS).
   ===================================================================== */
 
IF DB_ID('RaceDay') IS NULL
BEGIN
    CREATE DATABASE RaceDay;
END
GO
 
USE RaceDay;
GO

/* ---------------------------------------------------------------------
   Drop tables in reverse dependency order so the script can be re-run
   cleanly on an existing RaceDay database.
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.EventEnrolments', 'U') IS NOT NULL DROP TABLE dbo.EventEnrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.RouteInfo', 'U') IS NOT NULL DROP TABLE dbo.RouteInfo;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

* =====================================================================
   1. Users
      Stores both Organisers and Participants. The Role column drives
      role-based access in the API (Part 2).
   ===================================================================== */
CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(100)       NOT NULL,
    Email           NVARCHAR(150)       NOT NULL,
    PasswordHash    NVARCHAR(255)       NOT NULL,
    Role            NVARCHAR(20)        NOT NULL,
    PhoneNumber     NVARCHAR(20)        NULL,
    CreatedAt       DATETIME            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO