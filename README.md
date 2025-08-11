LedgerTransactionsAPI

API bancaria con doble partida, idempotencia, outbox (exactly-once hacia afuera), paginación por cursor, concurrencia segura y FX multimoneda. Construida en .NET 8 + EF Core + PostgreSQL.
Tabla de contenidos
•	Arquitectura
•	Decisiones (ADR corto)
•	Requisitos
•	Configuración
•	Cómo correr (dev)
•	Base de datos: migraciones y seed
•	Autenticación y autorización
•	Idempotencia
•	Paginación por cursor
•	FX Multimoneda
•	Outbox y Webhooks
•	Endpoints principales
•	Ejemplos de uso (curl)
•	Pruebas
•	Concurrencia (stress test de retiros)
•	Postman
•	SLOs y observabilidad
•	Siguientes pasos (Docker Compose)



Flujo de una transferencia
1.	Lock pesimista de las dos cuentas con orden consistente (por Id) para evitar deadlocks.
2.	Validaciones: mismas cuentas no, saldo suficiente, moneda origen = req.Currency.
3.	Multimoneda: si difiere, usa tasa IFxRates → acredita monto convertido.
4.	Registra 2 transacciones (una por cuenta) y 2 ledger entries (doble partida).
5.	Escribe evento en domain_outbox (misma transacción).
6.	Commit ACID.
7.	OutboxPublisher publica el evento al webhook y marca como published.


Decisiones (ADR corto)
•	Locking: Pessimistic locking (SELECT ... FOR UPDATE) para evitar doble débito en alta concurrencia. Ordenamos locks por Id para evitar deadlocks.
•	Idempotencia: Header Idempotency-Key. Guardamos clave, request-hash y resultado mínimo para responder igual a reintentos.
•	Outbox: Tabla domain_outbox con publisher en background (patrón Outbox → exactly-once outward).
•	Paginación: Keyset (cursor) por (Date, Id) para páginas estables bajo concurrencia (sin huecos que produce el offset).
•	FX: IFxRates (stub) + valuación a moneda base (DOP) en ledger_entries (BaseDebit/BaseCredit/BaseCurrency/FxRate) y asiento de rounding si hay difs de redondeo → asegura sum(BaseDebit) == sum(BaseCredit).
________________________________________
Requisitos
•	.NET 8 SDK
•	PostgreSQL 16 (local)
•	(Opcional) Docker Desktop (para Compose en el siguiente paso)

Cómo correr

dotnet restore
dotnet build
dotnet run --project ./LedgerTransactionsAPI/LedgerTransactionsAPI.csproj

Base de datos: migraciones y seed

# con Package Manager Console
Add-Migration InitialSchema
Update-Database

# luego, cuando agregaste valuación FX:
Add-Migration AddFxValuation
Update-Database

Autenticación y autorización
{ "username": "auditor", "password": "auditor123" }
