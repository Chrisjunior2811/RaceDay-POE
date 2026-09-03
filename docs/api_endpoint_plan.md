# RaceDay — API Endpoint Plan
### Part 1, Section B

This table plans every endpoint the RaceDay API will expose in Part 2. It covers Authentication, User Profile, Events, Categories, Event Enrolments, and Results, as required.

**Roles:** `None` = public/unauthenticated, `Any` = any logged-in user, `Organiser` = logged-in user with Role = Organiser, `Participant` = logged-in user with Role = Participant.

---

## 1. Authentication

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None (public) | `{ fullName, email, password, role, phoneNumber }` | 201 Created – new user object (no password) <br>400 Bad Request – validation failed <br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and issues a JWT for subsequent requests. | None (public) | `{ email, password }` | 200 OK – JWT token + user summary <br>401 Unauthorized – invalid credentials |

## 2. User Profile

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK – user profile object <br>401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any (logged in) | `{ fullName, phoneNumber }` | 200 OK – updated profile <br>400 Bad Request |
| GET | /api/users/me/history | Returns the logged-in participant's personal performance history (past results). | Participant | None | 200 OK – array of past results <br>404 Not Found – no history yet |

## 3. Events

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events so participants can browse. | None (public) | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns full details for one event, including its route info. | None (public) | None | 200 OK – event details object <br>404 Not Found |
| POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | `{ eventName, eventType, eventDate, location, description, route: {...} }` | 201 Created – created event <br>400 Bad Request |
| PUT | /api/events/{id} | Updates an event owned by the logged-in organiser. | Organiser | `{ eventName, eventDate, location, description }` | 200 OK – updated event <br>403 Forbidden – not the owner <br>404 Not Found |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in organiser. | Organiser | None | 204 No Content <br>403 Forbidden <br>404 Not Found |

## 4. Categories

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/events/{id}/categories | Lists all entry categories for a specific event. | None (public) | None | 200 OK – array of categories <br>404 Not Found |
| POST | /api/events/{id}/categories | Adds a new entry category to an event. | Organiser | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 201 Created – created category <br>403 Forbidden <br>404 Not Found |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 200 OK – updated category <br>403 Forbidden <br>404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category. | Organiser | None | 204 No Content <br>403 Forbidden <br>404 Not Found <br>409 Conflict – category already has enrolments |

## 5. Event Enrolments

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/categories/{id}/enrol | Enrols the logged-in participant into a category and issues a bib number. | Participant | `{ }` (emergency contact optional) | 201 Created – enrolment record with bib number <br>404 Not Found – category does not exist <br>409 Conflict – category full or already enrolled |
| GET | /api/enrolments/me | Lists the logged-in participant's own enrolments. | Participant | None | 200 OK – array of enrolments |
| GET | /api/events/{id}/enrolments | Lists all enrolments for an event (organiser management view). | Organiser | None | 200 OK – array of enrolments <br>403 Forbidden |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. | Participant (own) or Organiser | None | 204 No Content <br>403 Forbidden <br>404 Not Found |

## 6. Results

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/result | Captures a race result against a participant's enrolment. | Organiser | `{ finishTimeSeconds, finishPosition, categoryPosition, status }` | 201 Created – result record <br>403 Forbidden <br>404 Not Found <br>409 Conflict – result already captured |
| GET | /api/events/{id}/results | Returns the full results list for an event. | None (public) | None | 200 OK – array of results <br>404 Not Found |
| GET | /api/results/{id} | Returns a single result record. | None (public) | None | 200 OK – result object <br>404 Not Found |