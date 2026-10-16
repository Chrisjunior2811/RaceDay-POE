# RaceDay-POE
RaceDay — event management system for SA road events. PROG6212 Portfolio of Evidence.

## Description

RaceDay is a full-stack event management system being built for the
South African road running, walking, and cycling community. It's
designed to replace the paper-based registration, spreadsheets, and
disconnected communication channels currently used to manage events
like park runs, marathons, and cycle tours.

The platform will allow **Event Organisers** to create and manage
events, entry categories, and participant results, while
**Participants** will be able to browse upcoming events, enter events,
track their personal performance history, and prepare for race day
using live weather and route information.

This repository currently contains **Part 1 — System Planning and
Database** of the Portfolio of Evidence: an Entity Relationship
Diagram (ERD), an API endpoint plan, and a SQL Server database schema
for the RaceDay system. No application code has been written yet, as
per the Part 1 brief.

## Roles

RaceDay is a role-based system with two types of users:

### Event Organiser
- Registers and logs in to a personal account.
- Creates, updates, and deletes their own events.
- Adds and manages entry categories for each event (e.g. distance,
  entry fee, participant limit).
- Views and manages the list of participants enrolled in their events.
- Captures race results against participant enrolments once an event
  has taken place.

### Participant
- Registers and logs in to a personal account.
- Browses upcoming events and their categories.
- Enrols in an event category and receives a bib number.
- Views and cancels their own enrolments.
- Tracks their personal performance history across past events.
- Views live weather and route information to prepare for race day.

<img width="1365" height="655" alt="Screenshot 2026-09-03 230404" src="https://github.com/user-attachments/assets/d159b4ab-f9b8-440c-8fb3-7829743ad81d" />
RaceDay API — Part 2

1. Project Overview

RaceDay API is an ASP.NET Core Web API developed to manage running, walking and cycling events in South Africa. Part 2 implements the backend functionality using the database design and endpoint plan established in Part 1.

The API allows event organisers to manage events, categories, enrolments, payments and results. Participants can register, browse events, enrol in categories and view their results.

2. Technologies Used

C# and ASP.NET Core 8

Entity Framework Core

Microsoft SQL Server

JWT Bearer Authentication

BCrypt password hashing

Swagger/OpenAPI

xUnit for automated testing

GitHub Actions for continuous integration

3. Project Structure

Part2/
├── RaceDay.sln
├── RaceDay.Api/
│   ├── Controllers/
│   ├── Data/
│   ├── Dtos/
│   ├── Mapping/
│   ├── Migrations/
│   ├── Models/
│   ├── Services/
│   ├── appsettings.json
│   └── RaceDay.Api.csproj
├── RaceDay.Tests/
│   └── RaceDay.Tests.csproj
└── .github/
    └── workflows/
        └── dotnet.yml

Folder descriptions:

Controllers: Handles incoming API requests and responses.

Data: Contains the Entity Framework Core database context.

Dtos: Defines the data transferred between clients and the API.

Mapping: Handles conversion between models and DTOs.

Migrations: Contains database schema migration files.

Models: Represents the database entities.

Services: Contains reusable application services, including token generation.

RaceDay.Tests: Contains automated tests for the API.

4. Main Features

Authentication and authorisation

Users can register and log in using their credentials. Passwords are hashed using BCrypt, and successful login returns a JWT access token.

The API supports two roles:

Organiser: Manages their own events, categories, enrolments, payment statuses and event results.

Participant: Views events, enrols in categories, manages their profile and views their results.

Organiser registration requires a configured registration code. Role-based authorisation and ownership checks restrict access to protected operations.

Event management

Organisers can create, update and delete their own events. Event information includes route details and event categories.

Enrolments and payments

Participants can enrol in available categories and view their own enrolments. Organisers can view enrolments and update payment statuses for their events.

Results management

Organisers can capture and view results for their events. Participants can view their own results.

API documentation

Swagger provides an interactive interface for viewing available endpoints and testing API requests.

5. API Endpoints

Method

Endpoint

Purpose

POST

/api/auth/register

Register a user

POST

/api/auth/login

Log in and obtain a JWT

GET

/api/users/me

View own profile

PUT

/api/users/me

Update own profile

GET

/api/events

List events

GET

/api/events/{id}

View event details

POST

/api/events

Create an event

PUT

/api/events/{id}

Update an event

DELETE

/api/events/{id}

Delete an event

GET

/api/events/{eventId}/categories

View event categories

POST

/api/events/{eventId}/categories

Add a category

PUT

/api/categories/{id}

Update a category

DELETE

/api/categories/{id}

Delete a category

POST

/api/enrolments

Enrol in a category

GET

/api/enrolments/mine

View own enrolments

GET

/api/events/{eventId}/enrolments

View event enrolments

PUT

/api/enrolments/{id}/payment

Update payment status

POST

/api/results/{enrolmentId}

Capture an event result

GET

/api/results/mine

View own results

GET

/api/events/{eventId}/results

View event results

Access to endpoints depends on authentication, user roles and event ownership where applicable.

6. Requirements

Before running the project, install:

Visual Studio 2022 with the ASP.NET and web development workload

.NET 8 SDK

SQL Server LocalDB or another supported SQL Server instance

7. Installation and Setup

Download or clone the repository containing Part 2.

Open RaceDay.sln in Visual Studio.

Open RaceDay.Api/appsettings.json.

Configure ConnectionStrings:DefaultConnection for your SQL Server instance.

Replace the development JWT signing key with a secure secret and configure a private organiser registration code.

Restore the NuGet packages.

Build the solution to check for compilation errors.

Apply the Entity Framework Core migrations to create or update the database.

Set RaceDay.Api as the startup project.

Run the application.

To apply migrations from the Package Manager Console in Visual Studio, use:

Update-Database

If the project does not contain an initial migration, create one first:

Add-Migration InitialCreate
Update-Database

Use the existing migrations if they are already included. Do not create a duplicate initial migration.

8. Testing the API

After starting the application, open Swagger at:

http://localhost:5080/swagger

The HTTPS address may also be available depending on the selected launch profile.

To test authenticated endpoints:

Register a participant using POST /api/auth/register.

Log in using POST /api/auth/login.

Copy the JWT returned by the API.

Select Authorize in Swagger and enter the token as required by the Bearer authentication interface.

Test endpoints permitted for the logged-in user.

Register an organiser using the configured organiser registration code and test organiser-specific functionality.

Run automated tests

Open a terminal in the directory containing RaceDay.sln and execute:

dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release

Review the test results and resolve any failures before submission.

9. GitHub Actions

The GitHub Actions workflow is configured to restore, build and test the .NET solution when changes are pushed to the configured branches or when pull requests are opened.

To verify the workflow:

Push the latest Part 2 changes to GitHub.

Open the repository's Actions tab.

Open the latest workflow run.

Confirm that the build and test steps pass.

Capture a screenshot of the successful workflow for submission.

10. Security Considerations

Passwords must be stored as BCrypt hashes, not plain text.

JWT signing keys and organiser registration codes must remain private.

Protected endpoints must enforce authentication and the appropriate user role.
11. Relationship to Part 1

Part 2 implements the backend API using the database entities, relationships and endpoint requirements established in Part 1. The implementation should remain consistent with the approved entity-relationship diagram and SQL database design.

Organisers must only manage resources they are authorised to access.

Database connection strings and production secrets should be configured securely.

11. Relationship to Part 1

