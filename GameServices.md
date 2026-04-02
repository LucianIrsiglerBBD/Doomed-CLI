# Game Lobby API Server

This project is a Spring Boot–based API server for managing multiplayer game lobbies, users, maps, weapons, and per‑player game information. It provides endpoints and services to create, join, start, and manage lobbies, as well as track player stats and equipment.

---

## Features

- **Lobby Management**: Create, join, start, end, and delete lobbies.
- **User Management**: Register users, assign them to lobbies, and manage host reassignment.
- **Game Map Management**: Store maps with dimensions and metadata.
- **Weapon Management**: Create, fetch, damage, and delete weapons.
- **Player Stats**: Track per‑player health, speed, and position inside lobbies.

---

## Tech Stack

- **Java 26**
- **Spring Boot**
- **Spring Data JPA / Hibernate**
- **Lombok**
- **PostgreSQL**

---

## Domain Models

### `GameMap`

Represents a playable map in the game.

- `id` (PK)
- `name` (unique, required)
- `data` (map data string, optional)
- `width`, `height` (map dimensions)
- `createdAt`

---

### `Lobby`

Represents a multiplayer lobby.

- `id` (PK)
- `code` (unique, required)
- `started` (boolean flag)
- `hostUser` (FK → User)
- `map` (FK → GameMap)
- `startTime`, `endTime`
- `createdAt`, `updatedAt`
- **Relationships**: Many users can join the same lobby.

---

### `User`

Represents a player.

- `id` (PK)
- `name` (unique, required)
- `email` (unique, required)
- `lobby` (FK → Lobby, ManyToOne)
- `weapon` (FK → Weapon, OneToOne)
- `createdAt`

---

### `Weapon`

Represents a weapon that can be equipped by a user.

- `id` (PK)
- `name` (unique, required)
- `fireRate` (non‑negative)
- `damage` (non‑negative)
- `data` (optional metadata)
- `createdAt`, `updatedAt`

---

### `UsersGameInformation`

Represents per‑player stats inside a lobby.

- `id` (PK)
- `lobby` (FK → Lobby)
- `user` (FK → User)
- `speed` (non‑negative)
- `x`, `y` (coordinates within map bounds)
- `health` (0–100, default 100)
- `createdAt`, `updatedAt`

---

## Services

### `LobbyService`

- `createLobby(hostUserId, mapId, durationMinutes)`
- `joinLobby(userId, lobbyCode)`
- `startLobby(lobbyId, hostUserId)`
- `endLobby(lobbyId)`
- `leaveLobby(userId, lobbyId)`
- `deleteLobby(lobbyId, userId)`
- `getActiveLobbies()`, `getAllLobbies()`

### `UserService`

- `getUserById(id)`, `getUserByName(name)`
- `getAllUsers()`
- `getUserLobby(userId)`
- `joinLobby(userId, lobbyCode)` → delegates to `UsersGameInfoService`
- `leaveLobby(userId)` → handles host reassignment
- `hasActiveLobby(userId)`
- `getUsersInLobby(lobbyId)`
- `deleteUser(userId)`

### `UsersGameInfoService`

- `createUsersGameInformation(userId, lobbyId, speed, x, y)`
- `updateUsersGameInformation(userId, lobbyId, speed, health, x, y)`
- `getUsersGameInformation(userId, lobbyId)`
- **Validation**:
  - `validatePosition(lobby, x, y)`
  - `validateStats(speed, health)`

### `WeaponService`

- `getWeapons()`
- `getWeaponById(weaponId)`
- `createWeapon(newWeapon)` → validates and checks duplicates
- `damageWeapon(weaponId)` → decrements damage
- `deleteWeapon(weaponId)`

---

## Running the Project

1. Clone the repository:

   ```bash
   git clone https://github.com/your-org/game-lobby-api.git
   cd game-lobby-api
   ```

2. Build and run:

   ```bash
   ./mvnw spring-boot:run
   ```

3. Access API at:

   ```
   http://localhost:8080
   ```

---

## Example Flow

1. **Create Lobby** → Host creates a lobby with a map and duration.
2. **Join Lobby** → Users join using the lobby code.
3. **Start Lobby** → Host starts the lobby once at least 2 players are present.
4. **Play & Update Stats** → Users move, take damage, and update stats.
5. **End Lobby** → Lobby ends when time expires or host ends it.

---

## Notes

- Max players per lobby: **50**
- Lobby codes: **6 characters (A–Z, 0–9)**
- Health range: **0–100**
- Weapons must have non‑negative fire rate and damage.

---

## License

MIT License – free to use and modify.

---
