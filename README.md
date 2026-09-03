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

