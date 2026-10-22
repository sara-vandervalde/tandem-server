# Tandem – שרת

**Tandem** היא מערכת לארגון אירועים משפחתיים וחברתיים בקבוצה: ימי הולדת, ארוחות חג, מסיבות.
קבוצה (למשל "המשפחה") מוסיפה אירוע ללוח השנה. חבר אחד לוקח אחריות על האירוע, והחברים מתחלקים בתחומים:
**המקום, המתנה והארוחה**, ורשימת **"מה להביא"** (אוכל, כלים). כשהכול מכוסה, האירוע עובר לבד למצב "מאורגן".
אחרי האירוע, האחראי סוגר את החשבון: המערכת סוכמת מה כל אחד שילם, מחלקת לפי שיטת החלוקה,
ומחשבת מי מעביר כמה ולמי. כל חבר רואה למי לשלם, את פרטי החשבון של המקבל, ומסמן "שילמתי".

שרת ASP.NET Core Web API (.NET 9), עם PostgreSQL ו-Entity Framework Core.
הלקוח (React) נמצא ב-repository נפרד: `tandem-client`.

---

## המשאב המוגבל והטיפול בתחרות

**המשאב המוגבל:** תחום אחריות באירוע – האירוע עצמו, המקום, המתנה, הארוחה – וכל פריט ברשימת "מה להביא".
לכל אחד מהם יכול להיות **אחראי אחד בלבד**.

**איפה התחרות:** שני חברים רואים שהמקום פנוי ולוחצים "אני לוקח/ת" באותו רגע.
שתי הבקשות עוברות את הבדיקה "האם פנוי?", ובלי הגנה – השנייה הייתה דורסת את הראשונה בשקט.

**איך המערכת מגיבה – Optimistic Concurrency:**
1. לכל ישות שמתחרים עליה (`Event`, `Venue`, `Gift`, `Meal`, `EventItem`) יש concurrency token:
   `public uint Version`, ובקובץ ה-Configuration – `IsRowVersion()`. Npgsql ממפה אותו לעמודת המערכת `xmin` של PostgreSQL,
   שמשתנה אוטומטית בכל עדכון של השורה.
2. EF Core מוסיף את הגרסה ל-UPDATE: `... WHERE "Id" = @id AND xmin = @version`.
   הראשון מעדכן שורה אחת. אצל השני הגרסה כבר השתנתה, 0 שורות מתעדכנות, ו-EF זורק `DbUpdateConcurrencyException`.
3. ה-Repository (Data) מתרגם אותה ל-`ConcurrencyConflictException` (Core), כך שה-Service לא תלוי ב-EF.
4. ה-Service מחזיר **409 Conflict** עם הודעה ברורה בעברית ("חבר/ה אחר/ת כבר לקח/ה אחריות על המקום"),
   ורושם **Warning** בלוג – זה מצב צפוי, לא תקלה.

**ההוכחה:** `Tandem.Tests/Concurrency/ConcurrencyTokenTests.cs` – שני `DbContext` נפרדים קוראים את אותו מקום,
שניהם תופסים אותו, הראשון נשמר והשני מקבל חריגה. רץ מול PostgreSQL אמיתי, בבסיס נתונים נפרד (`tandem_tests`).

---

## ארכיטקטורה

| פרויקט | תפקיד | מפנה אל |
| --- | --- | --- |
| `Tandem.Core` | ישויות, DTOs, ממשקים, `Result`, חריגות. בלי שום חבילה חיצונית | – |
| `Tandem.Data` | `AppDbContext`, Configurations (Fluent API), Migrations, Seed, Repositories | Core |
| `Tandem.Service` | לוגיקה עסקית, הרשאות, AutoMapper, `EventStatusRules`, `SettlementCalculator` | Core |
| `Tandem.API` | Controllers, Middleware, JWT, Swagger, NLog, רישום DI | Core, Service, Data (רק לרישום) |
| `Tandem.Tests` | xUnit + Moq על ה-Service, ובדיקת התחרות מול בסיס נתונים | הכול |

Controller לא מכיר `DbContext` – רק Service. Service לא מכיר EF – רק ממשקי Repository מ-Core.

---

## הרצה מקומית

**נדרש:** .NET 9 SDK, ו-**PostgreSQL** (גרסה 14 ומעלה) שרץ מקומית.

```bash
git clone <repository-url> && cd tandem-server

# סודות – רק ב-User Secrets, אף פעם לא בקוד או ב-appsettings.json
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=tandem;Username=postgres;Password=<הסיסמה שלך>" --project Tandem.API
dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית של 32 תווים לפחות>" --project Tandem.API

dotnet run --project Tandem.API
```

- **לא צריך להריץ שום פקודה על בסיס הנתונים:** בעליית השרת, `Database.MigrateAsync()` יוצר את בסיס הנתונים,
  מריץ את כל ה-Migrations ומכניס את נתוני הדמו (Seed).
- Swagger: http://localhost:5223/swagger – ללחוץ **Authorize** ולהדביק את ה-token מ-`POST /api/auth/login`.
- בדיקות: `dotnet test` (בדיקות התחרות יוצרות לעצמן בסיס נתונים נפרד, `tandem_tests`, על אותו שרת).
- לוגים: קונסולה ו-`Tandem.API/logs/` (NLog). כל שורה כוללת CorrelationId, שחוזר גם ב-header `X-Correlation-Id`.

### משתמשי דמו

| Role | אימייל | סיסמה | מה יש לו |
| --- | --- | --- | --- |
| Admin | `admin@example.com` | `Admin123!` | רואה את כל המשתמשים והקבוצות, חוסם ומשחרר משתמשים |
| Member | `sara@example.com` | `Member123!` | מנהלת הקבוצה "המשפחה", אחראית האירוע "יום ההולדת של סבתא" |
| Member | `rachel@example.com` | `Member123!` | חברה בקבוצה "המשפחה" |

**לנסות את התחרות:** להתחבר כשרה ב-Swagger אחד וכרחל באחר (חלון גלישה בסתר),
ושתיהן `PUT /api/events/1/venue/responsible`. הראשונה מקבלת 200, השנייה **409**.

---

## Endpoints עיקריים

| תחום | Endpoints |
| --- | --- |
| הרשמה והתחברות | `POST /api/auth/register` (201), `POST /api/auth/login` |
| קבוצות והזמנות | `GET/POST/PUT/DELETE /api/groups`, `PUT /api/groups/{id}/manager`, `DELETE /api/groups/{id}/members/{userId}`, `POST /api/groups/{id}/invitations`, `GET /api/me/invitations`, `PATCH /api/invitations/{id}` |
| אירועים | `GET /api/events?groupId=&status=&search=&from=&to=&sortBy=&sortDir=&page=&pageSize=`, `GET /api/groups/{id}/events?from=&to=` (לוח שנה), `GET/POST/PUT/PATCH/DELETE /api/events/{id}`, `PUT /api/events/{id}/participants` |
| **המשאב המוגבל** | `PUT /api/events/{id}/organizer`, `PUT/DELETE /api/events/{id}/{venue\|gift\|meal}/responsible` → 200 / **409** |
| פרטי תחום | `PUT /api/events/{id}/venue`, `/gift`, `/meal` (כתובת, תיאור, סוג ארוחה, מחיר) |
| "מה להביא" | `GET/POST /api/events/{id}/items`, `DELETE /api/items/{id}`, `PUT/DELETE /api/items/{id}/assignee` → 200 / **409** |
| בנקים | `GET /api/banks`, `GET/PUT /api/me/bank-account` |
| חשבון ותשלומים | `GET /api/events/{id}/settlement-preview`, `POST /api/events/{id}/settlements` (201), `GET /api/events/{id}/summary`, `GET /api/me/settlements`, `PATCH /api/settlements/{id}` |
| ניהול (Admin) | `GET /api/users?page=&pageSize=`, `GET /api/users/{id}`, `PATCH /api/users/{id}` |

שגיאות חוזרות תמיד כ-ProblemDetails, עם הודעה בעברית: 400 (ולידציה), 401, 403, 404, 409.

---

## הערת אבטחה: AutoMapper 14.0.0

ה-Build מציג אזהרה על פרצה ידועה ב-AutoMapper 14 ([GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x)):
מניעת שירות (DoS) במיפוי של מבנה נתונים מקונן לעומק רב מאוד.
**הסיכון כאן נמוך:** AutoMapper ממפה רק את הישויות שלנו ל-DTOs. הן נטענות מבסיס הנתונים במבנה קבוע ורדוד,
וקלט מהמשתמש לא עובר דרכו (בקשות נכנסות ממופות ידנית ב-Service).
התיקון הוא גרסה 15 ומעלה, שדורשת מפתח רישיון (חינמי לשימוש לא מסחרי). במערכת אמיתית – משדרגים.
