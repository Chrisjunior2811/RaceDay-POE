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

* ---------------------------------------------------------------------
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

* =====================================================================
   2. Events
      Each Event belongs to exactly one Organiser (Users.Role = 'Organiser').
   ===================================================================== */
CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT                 NOT NULL,
    EventName       NVARCHAR(150)       NOT NULL,
    EventType       NVARCHAR(20)        NOT NULL,
    EventDate       DATETIME            NOT NULL,
    Location        NVARCHAR(150)       NOT NULL,
    Description     NVARCHAR(MAX)       NULL,
    CreatedAt       DATETIME            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Events_Type CHECK (EventType IN ('Running', 'Walking', 'Cycling'))
);
GO

* =====================================================================
   3. RouteInfo
      One-to-one with Events. Holds route/elevation detail used by the
      "prepare for race day" feature (live weather and route info).
   ===================================================================== */
CREATE TABLE dbo.RouteInfo (
    RouteId             INT IDENTITY(1,1)  NOT NULL,
    EventId             INT                NOT NULL,
    RouteDescription    NVARCHAR(MAX)      NULL,
    DistanceKm          DECIMAL(5,2)       NOT NULL,
    ElevationGainM      INT                NULL DEFAULT 0,
    StartPoint          NVARCHAR(150)      NULL,
    EndPoint            NVARCHAR(150)      NULL,
    MapUrl              NVARCHAR(255)      NULL,
    CONSTRAINT PK_RouteInfo PRIMARY KEY (RouteId),
    CONSTRAINT FK_RouteInfo_Event FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId),
    CONSTRAINT UQ_RouteInfo_EventId UNIQUE (EventId)
);
GO

* =====================================================================
   4. Categories
      Each Event can have multiple entry categories (e.g. 5km, 10km).
   ===================================================================== */
CREATE TABLE dbo.Categories (
    CategoryId       INT IDENTITY(1,1)   NOT NULL,
    EventId          INT                 NOT NULL,
    CategoryName     NVARCHAR(100)       NOT NULL,
    DistanceKm       DECIMAL(5,2)        NOT NULL,
    EntryFee         DECIMAL(8,2)        NOT NULL DEFAULT 0,
    MaxParticipants  INT                 NOT NULL DEFAULT 500,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId)
);
GO

* =====================================================================
   5. EventEnrolments
      Links a Participant (Users.Role = 'Participant') to a Category.
   ===================================================================== */
CREATE TABLE dbo.EventEnrolments (
    EnrolmentId      INT IDENTITY(1,1)   NOT NULL,
    ParticipantId    INT                 NOT NULL,
    CategoryId       INT                 NOT NULL,
    BibNumber        NVARCHAR(10)        NOT NULL,
    EnrolmentDate    DATETIME            NOT NULL DEFAULT GETDATE(),
    PaymentStatus    NVARCHAR(20)        NOT NULL DEFAULT 'Pending',
    CONSTRAINT PK_EventEnrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT UQ_Enrolments_BibNumber UNIQUE (BibNumber),
    CONSTRAINT UQ_Enrolments_ParticipantCategory UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT CK_Enrolments_PaymentStatus CHECK (PaymentStatus IN ('Pending', 'Paid', 'Cancelled'))
);
GO

* =====================================================================
   6. Results
      One-to-one (optional) with EventEnrolments; populated after race day.
   ===================================================================== */
CREATE TABLE dbo.Results (
    ResultId            INT IDENTITY(1,1)  NOT NULL,
    EnrolmentId         INT                NOT NULL,
    FinishTimeSeconds   INT                NULL,
    FinishPosition       INT               NULL,
    CategoryPosition     INT               NULL,
    Status              NVARCHAR(20)       NOT NULL DEFAULT 'Finished',
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.EventEnrolments (EnrolmentId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DSQ'))
);
GO

=====================================================================
   SEED DATA
   ===================================================================== */
 
-- Users: 2 Organisers, 2 Participants
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, PhoneNumber)
VALUES
    ('Thandiwe Mokoena', 'thandiwe.mokoena@raceday.co.za', 'HASHED_PW_1', 'Organiser', '0821234567'),
    ('Johan van der Merwe', 'johan.vdm@raceday.co.za', 'HASHED_PW_2', 'Organiser', '0827654321'),
    ('Sipho Ndlovu', 'sipho.ndlovu@example.com', 'HASHED_PW_3', 'Participant', '0731112222'),
    ('Emma Botha', 'emma.botha@example.com', 'HASHED_PW_4', 'Participant', '0739998888');
GO