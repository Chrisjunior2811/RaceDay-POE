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
 