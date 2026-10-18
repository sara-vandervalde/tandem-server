# Tandem – תוכנית עבודה לפרויקט

עודכן: 18.10.2026 – הזמנות לקבוצה, רשימת "מה להביא" (אוכל וכלים), ארוחה שמכינים לבד, לוח הודעות, תשתית מיילים; Swagger (Swashbuckle), AutoMapper 14, endpoint לביטול אירוע, מעבר לקייטרינג, CorrelationId בלוג השגיאות

---

## 1. סקירה

Tandem היא מערכת לארגון אירועים בקבוצה: לוח שנה לכל קבוצה, חלוקת אחריות על האירוע, המתנה והארוחה, וחישוב אוטומטי של מי משלם למי.
המערכת מוגשת כפרויקט סיום בשני קורסים: **השרת נבדק בקורס דוטנט**, ו**הלקוח נבדק בקורס ריאקט**.

| נושא | החלטה |
| --- | --- |
| שרת | ASP.NET Core Web API, .NET 9, Controllers |
| לקוח | React + TypeScript עם Vite |
| בסיס נתונים | **PostgreSQL**, מותקן מקומית, דרך `Npgsql.EntityFrameworkCore.PostgreSQL`. נבחר פעם אחת ולא משתנה |
| מיקום במחשב | `C:\PROJECKT\Tandem` – נתיב באנגלית, בלי רווחים |
| Repositories | `tandem-server` (קיים, Private), `tandem-client` (ייפתח בשלב R1) |
| משאב מוגבל | תפקידי אחריות באירוע (אירוע, מקום, מתנה, ארוחה) ופריטים ברשימת "מה להביא", עם Optimistic Concurrency |

### עקרונות מנחים

- **השרת קודם, בפרוסות:** כל פעם תכונה שלמה – מהטבלה ועד ה-endpoint. הלקוח מתחיל אחרי שיש התחברות ואירועים עובדים.
- **עומק לפני רוחב:** מעט ישויות, שכבות נקיות, טיפול נכון בתחרות. מיילים ותזכורות – רק בסוף.
- **כל שלב מסתיים בבדיקת סיום ו-commit.** לא עוברים לשלב הבא לפניהם.

---

## 2. איך המערכת עובדת

תיאור מפורט מנקודת המבט של המשתמשות נמצא ב-`USER_FLOW.md`. כאן – הכללים.

1. **קבוצה:** משתמש יוצר קבוצה והופך למנהל שלה. **לכל קבוצה מנהל אחד בלבד.**
2. **הזמנת חברים:** המנהל מזמין לפי אימייל. לכל כתובת נוצרת הזמנה ממתינה; המוזמן רואה אותה בדף הבית (מיד, או אחרי שיירשם עם אותו אימייל) ומאשר או דוחה. הזמנה פגה אחרי 14 יום, והמנהל יכול לבטל הזמנה ממתינה. אין הוספת חבר ישירה.
3. **עזיבת קבוצה:** חבר רגיל יכול לעזוב, אלא אם יש לו אחריות באירוע פתוח – אז עליו לשחרר אותה קודם. **מנהל הקבוצה חייב להעביר את הניהול לחבר אחר לפני שהוא עוזב.** מנהל שהוא החבר היחיד יכול רק למחוק את הקבוצה. אותם כללים חלים כשהמנהל מסיר חבר.
4. **אירוע בלוח:** כל חבר יכול להוסיף אירוע ללוח השנה, ומיד נשאל אם הוא אחראי האירוע. מי שמאשר ראשון – הוא האחראי. אם היוצר ענה "לא", האירוע נשאר טיוטה וכל חבר יכול לתפוס את האחריות. **טיוטה יכולים למחוק מי שיצר אותה או מנהל הקבוצה.**
5. **הגדרת האירוע:** אחראי האירוע בוחר **Solo** (רק הוא תופס את תחומי האחריות) או **Open** (כל חבר יכול לתפוס); האם יש מתנה (`HasGift`); ושיטת החלוקה: שווה בשווה בין **כל חברי הקבוצה**, או שהוא משלם לבד.
6. **שלושה תחומי אחריות:** מקום וארוחה (חובה), ומתנה (רק אם `HasGift`). חבר יכול לתפוס יותר מתחום אחד.
   - **אחראי המקום** מזין כתובת ומחיר, ומוסיף לרשימה פריטי **כלים ועריכה** (צלחות, מפות, קישוט) – אם צריך.
   - **אחראי המתנה** מזין תיאור ומחיר.
   - **אחראי הארוחה** בוחר אחד משלושה סוגים:
     - **ארוחה משותפת** – מוסיף לרשימה פריטי **אוכל**, וכל חבר תופס פריט ומביא אותו, על חשבונו.
     - **קייטרינג** – מזין שם קייטרינג ומחיר.
     - **מכין לבד** – מכין את כל האוכל בעצמו. הוא יכול לבנות לעצמו תפריט, אבל **התפריט פרטי**: רק הוא רואה אותו, ואף אחד לא תופס ממנו. מזין מחיר.
     - **מעבר לקייטרינג** חסום כל עוד יש ברשימה פריטי אוכל ← 409 "יש למחוק את פריטי האוכל לפני המעבר לקייטרינג".
7. **רשימת "מה להביא":** לכל אירוע רשימה אחת של פריטים, מסוג אוכל או כלים. פריטים ציבוריים – **כל חבר בקבוצה יכול לתפוס, בכל מצב, גם ב-Solo.** פריטי אוכל בארוחה "מכין לבד" פרטיים לאחראי הארוחה.
8. **מחיר:** יכול להיות **0**. מחיר ריק = עוד לא הוזן. מי שאחראי על תחום הוא מי ששילם עליו.
9. **אירוע מאורגן:** הסטטוס עובר **אוטומטית** ל-Organized כשיש אחראי אירוע; למקום, לארוחה ולמתנה (אם יש) יש אחראי ומחיר שהוזן; וכל הפריטים הציבוריים ברשימה נתפסו.
10. **לוח הודעות (תוספת):** בכל אירוע חברי הקבוצה יכולים לכתוב הודעות ("כדאי להוסיף עוגה ללא סוכר"). לא צ'אט בזמן אמת – העמוד מתרענן כל כמה שניות.
11. **סגירת חשבון:** אחראי האירוע סוגר ידנית, כשהאירוע מאורגן. המערכת סוכמת את מחירי המקום, המתנה והארוחה, מחלקת לפי שיטת החלוקה ומקזזת ממי ששילם. המחירים ננעלים, והסטטוס עובר ל-Settled. עד אז כל חבר רואה הערכה חיה של חלקו.
12. **תשלום:** כל חבר רואה כמה לשלם, למי, ואת פרטי החשבון של המקבל, ומסמן "שילמתי". אין העברה בנקאית מתוך המערכת.
13. **מיילים:** כל מקום שבו המערכת "שולחת מייל" (הזמנה לקבוצה, התראות, סגירת חשבון) עובר דרך הממשק `IEmailSender`. עד שלב 8 המימוש רק כותב ללוג; בשלב 8 מחליפים אותו בשליחה אמיתית.
14. **מנהל המערכת** רואה את כל המשתמשים והקבוצות, אבל לא משתתף בארגון אירועים של קבוצות שהוא לא חבר בהן.

### מחזור החיים של אירוע

| סטטוס | מתי |
| --- | --- |
| `Draft` | האירוע נוצר, עוד אין אחראי אירוע |
| `Organizing` | יש אחראי, החלוקה בתהליך |
| `Organized` | כל התחומים הנדרשים מאוישים, המחירים הוזנו וכל הפריטים הציבוריים נתפסו. מעבר אוטומטי; שחרור מחזיר ל-Organizing |
| `Settled` | החשבון נסגר ונוצרו התשלומים. המחירים נעולים |
| `Cancelled` | בוטל על ידי אחראי האירוע |

---

## 3. ארכיטקטורה

Solution אחד עם ארבעה פרויקטים, ופרויקט בדיקות חמישי.

| פרויקט | תוכן | מפנה אל |
| --- | --- | --- |
| `Tandem.Core` | Entities, Enums, Interfaces, DTOs, Result | אף פרויקט. **בלי חבילות חיצוניות** |
| `Tandem.Data` | AppDbContext, Configurations (Fluent API), Repositories, Migrations | Core |
| `Tandem.Service` | לוגיקה עסקית, בדיקות הרשאה, SettlementCalculator, AutoMapper Profiles | Core |
| `Tandem.API` | Controllers, Middleware, Program.cs, nlog.config | Core, Service, Data (לרישום ב-Program.cs בלבד) |
| `Tandem.Tests` | xUnit, Moq, בדיקת Concurrency | Core, Service, Data |

### מבנה תיקיות

```
Tandem/
├── Tandem.sln
├── Tandem.Core/
│   ├── Entities/
│   ├── Enums/
│   ├── Interfaces/      ← כולל IEmailSender
│   │   ├── Repositories/
│   │   └── Services/
│   ├── DTOs/
│   ├── Common/          ← Result<T>, PagedResult<T>
│   └── Exceptions/      ← ConcurrencyConflictException
├── Tandem.Data/
│   ├── AppDbContext.cs
│   ├── Configurations/  ← IEntityTypeConfiguration לכל ישות
│   ├── Repositories/
│   ├── Email/           ← LogEmailSender (עכשיו), SmtpEmailSender (שלב 8)
│   └── Migrations/
├── Tandem.Service/
│   ├── Services/
│   ├── Authorization/   ← GroupAccessService
│   ├── Settlement/      ← SettlementCalculator
│   └── Mapping/         ← AutoMapper Profiles
├── Tandem.API/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Extensions/      ← Result → HTTP status
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── nlog.config
└── Tandem.Tests/
```

### זרימת בקשה

Controller מקבל DTO ← קורא ל-Service ← ה-Service משתמש בממשקי Repository שמוגדרים ב-Core ← מחזיר `Result<T>` ← ה-Controller מתרגם את ה-Result לסטטוס HTTP.
ה-Controller אף פעם לא מכיר DbContext או Entity.

### כללים שהמורה בודקת

- Core לא מפנה לשום פרויקט, ולא לשום חבילת EF Core או Npgsql. (ה-concurrency token מוגדר ב-Fluent API ב-Data, ולכן הישות ב-Core היא מחלקה נקייה לגמרי.)
- כל התלויות מוזרקות דרך ה-constructor; אין `new` על Service בתוך Service.
- DbContext, Repositories ו-Services רשומים כ-**Scoped**.
- **להכין הסבר בעל פה:** Singleton שמחזיק Scoped נשאר עם DbContext אחד לכל חיי האפליקציה. DbContext אינו thread-safe, הוא צובר נתונים מיושנים, ובקשות במקביל ישברו אותו.

---

## 4. מודל ההרשאות

ההרשאות בנויות בשלוש רמות. מנהל קבוצה ואחראי אירוע **אינם** יכולים להחליף את דרישת שני ה-roles, כי הם תלויים במשאב: אותו משתמש יכול להיות מנהל בקבוצה אחת וחבר רגיל באחרת, ולכן אי אפשר לשים אותם ב-JWT. הם נבדקים בשכבת ה-Service.

1. **רמת מערכת (role ב-JWT):** `Admin`, `Member`. נבדק עם `[Authorize(Roles = "Admin")]`.
2. **רמת קבוצה (`Groups.ManagerId`):** מנהל אחד לכל קבוצה. יוצר הקבוצה הופך למנהל.
3. **רמת אירוע:** יוצר האירוע (`Events.CreatedById`), אחראי האירוע (`Events.OrganizerId`), ואחראי כל תחום (`ResponsibleId` ב-Venues, Gifts, Meals).

| פעולה | מי מורשה | סטטוס בכישלון |
| --- | --- | --- |
| צפייה בכל המשתמשים והקבוצות במערכת | Admin | 403 |
| מחיקת קבוצה כלשהי, חסימת משתמש | Admin | 403 |
| יצירת קבוצה | כל משתמש מחובר | 401 |
| הזמנת חברים, ביטול הזמנה, הסרת חבר, עריכת הקבוצה | מנהל הקבוצה | 403 |
| אישור או דחייה של הזמנה | המוזמן (לפי האימייל שלו) | 403 / 409 (כבר טופלה או פגה) |
| העברת ניהול לחבר אחר בקבוצה | מנהל הקבוצה | 403 (לא מנהל) / 400 (היעד אינו חבר) |
| עזיבת קבוצה | כל חבר; **מנהל – רק אחרי שהעביר ניהול**; מי שיש לו אחריות באירוע פתוח – רק אחרי ששחרר | 409 |
| צפייה בלוח השנה ובאירועים של קבוצה | חברי הקבוצה, Admin | 403 |
| יצירת אירוע בלוח | חברי הקבוצה | 403 |
| מחיקת טיוטה (אירוע בלי אחראי) | מי שיצר אותה, או מנהל הקבוצה | 403 |
| מחיקה או ביטול של אירוע עם אחראי | אחראי האירוע | 403 |
| אישור "אני אחראי האירוע" | חבר קבוצה, רק כשהמקום פנוי | 409 |
| עריכת אירוע, Solo/Open, `HasGift`, שיטת חלוקה | אחראי האירוע | 403 |
| תפיסת אחריות על מקום / מתנה / ארוחה | Open: כל חבר. Solo: אחראי האירוע בלבד | 403 / 409 |
| עדכון פרטים ומחיר של תחום | האחראי על אותו תחום, כשהאירוע לא Settled | 403 / 409 |
| הוספה ומחיקה של פריטי אוכל | אחראי הארוחה | 403 |
| הוספה ומחיקה של פריטי כלים ועריכה | אחראי המקום | 403 |
| צפייה בפריט פרטי (תפריט "מכין לבד") | אחראי הארוחה בלבד | 404 |
| תפיסת פריט ציבורי | כל חבר בקבוצה, **בכל מצב** (גם Solo) | 403 / 409 |
| שחרור פריט | מי שתפס אותו, או מי שהוסיף אותו (אחראי הארוחה / המקום) | 403 |
| כתיבה וקריאה בלוח ההודעות | חברי הקבוצה | 403 |
| סגירת חשבון | אחראי האירוע, כשהסטטוס Organized | 403 / 409 |
| סימון "שילמתי" | המשלם עצמו | 403 |
| צפייה בפרטי חשבון בנק | רק מי שחייב כסף לבעל החשבון | 403 |

כל בדיקות רמת הקבוצה ורמת האירוע מרוכזות במחלקה אחת, `GroupAccessService`, ולא מפוזרות בין ה-Services.

פריט פרטי מחזיר 404 ולא 403 לכל מי שאינו אחראי הארוחה: מבחינתו הפריט פשוט לא קיים, ואין סיבה לחשוף שיש תפריט נסתר.

---

## 5. מודל הנתונים

שלוש-עשרה טבלאות, חמש מהן עם concurrency token (`Version`, ממופה לעמודת המערכת `xmin`) לטיפול בתחרות. כל התאריכים ב-**UTC**; כל הסכומים `decimal(18,2)`. המבנה מבוסס על תכנון הטבלאות של הסטודנטית: **טבלה נפרדת לכל תחום אחריות**, עם אחראי ומחיר.

| טבלה | שדות | הערות |
| --- | --- | --- |
| **Users** | Id, FullName, Email, PasswordHash, Address?, Role, IsActive, CreatedAt | Email ייחודי. משתמשים לא נמחקים – רק נחסמים (`IsActive`) |
| **Groups** | Id, Name, ManagerId, CreatedAt | מנהל אחד לקבוצה. אין עמודת "מספר חברים" – מחושב ב-COUNT |
| **GroupMembers** | GroupId, UserId, JoinedAt | טבלת many-to-many. מפתח מורכב. המנהל תמיד גם חבר |
| **GroupInvitations** | Id, GroupId, Email, InvitedById, Status, CreatedAt, ExpiresAt, RespondedAt? | הזמנות. מקושרות לאימייל ולא למשתמש – כך אפשר להזמין מי שעוד לא נרשם |
| **Events** | Id, GroupId, CreatedById, Title, EventDate, OrganizerId?, OrganizationMode, HasGift, SplitMethod, Status, **Version** | |
| **Venues** | Id, EventId, ResponsibleId?, Address?, Price?, **Version** | one-to-one מול Events. נוצרת יחד עם האירוע |
| **Gifts** | Id, EventId, ResponsibleId?, Description?, Price?, **Version** | one-to-one מול Events. קיימת רק כש-`HasGift` |
| **Meals** | Id, EventId, ResponsibleId?, MealType?, CateringName?, Price?, **Version** | one-to-one מול Events. נוצרת יחד עם האירוע |
| **EventItems** | Id, EventId, Type, Name, IsPrivate, AddedById, AssigneeId?, **Version** | רשימת "מה להביא": אוכל וכלים. בלי מחיר |
| **EventMessages** | Id, EventId, UserId, Text, CreatedAt | לוח ההודעות. תוספת – migration שלישי |
| **Banks** | Id, Name | רשימה קבועה, ב-Seed |
| **BankAccounts** | Id, UserId, BankId, BranchNumber, AccountNumber, AccountHolderName | one-to-one מול Users. מידע רגיש – לא נכתב ללוג |
| **Settlements** | Id, EventId, FromUserId, ToUserId, Amount, IsPaid, PaidAt? | נוצרות בסגירת החשבון |

`?` = שדה nullable (יכול להיות ריק). ב-`Price`: ריק = עוד לא הוזן, `0` = בחינם.

**`IsPrivate` ב-EventItems:** `true` רק לפריטי אוכל בארוחה מסוג "מכין לבד". כשאחראי הארוחה משנה את סוג הארוחה, הפריטים שלו מתעדכנים בהתאם.

**כל קשר מוגדר בצד אחד בלבד** – בטבלת ה"ילד" (`EventId` ב-Venues, `UserId` ב-BankAccounts). אין עמודות הפוכות ב-Events או ב-Users.

### Enums (ב-`Tandem.Core/Enums`, קובץ לכל אחד)

| Enum | ערכים |
| --- | --- |
| `UserRole` | Admin, Member |
| `InvitationStatus` | Pending, Accepted, Declined, Cancelled |
| `OrganizationMode` | Solo, Open |
| `MealType` | Shared, Catering, SelfMade |
| `ItemType` | Food, Equipment |
| `SplitMethod` | Equal, OrganizerPays |
| `EventStatus` | Draft, Organizing, Organized, Settled, Cancelled |

הזמנה שפגה לא מקבלת סטטוס משלה: היא `Pending` שה-`ExpiresAt` שלה עבר, והשאילתות בודקות את התאריך. כך אין צורך בתהליך רקע שמעדכן סטטוסים.

סוג הארוחה הוא enum ולא טבלה: שלושה ערכים קבועים לא מצדיקים JOIN. רשימת הבנקים, לעומת זאת, היא טבלה – היא ארוכה ועשויה להשתנות.

### קשרים

- **one-to-many:** Group ← Events, Group ← GroupInvitations, Event ← EventItems, Event ← EventMessages, Event ← Settlements, Bank ← BankAccounts
- **many-to-many:** Users ↔ Groups דרך GroupMembers
- **one-to-one:** Event ↔ Venue, Event ↔ Gift, Event ↔ Meal, User ↔ BankAccount

### הגדרות Fluent API שאסור לשכוח

- `DeleteBehavior.Restrict` על כל מפתח זר ל-Users (Manager, CreatedBy, Organizer, InvitedBy, Responsible בשלוש הטבלאות, AddedBy, Assignee, FromUser, ToUser, ומחבר ההודעה), כדי שמחיקה לא תמחק בשרשרת אירועים ותשלומים.
- `Cascade` מ-Event אל Venue, Gift, Meal, EventItems ו-EventMessages: מחיקת אירוע מוחקת את כל מה ששייך לו.
- **אינדקס ייחודי על `EventId`** ב-Venues, Gifts ו-Meals, ועל `UserId` ב-BankAccounts – זה מה שהופך אותם ל-one-to-one, ומונע שתי שורות לאותו אירוע.
- **אינדקס ייחודי חלקי** על `(GroupId, Email)` ב-GroupInvitations, רק לשורות במצב Pending: `HasIndex(i => new { i.GroupId, i.Email }).IsUnique().HasFilter("\"Status\" = 0")`. כך אי אפשר לשלוח שתי הזמנות ממתינות לאותה כתובת, וגם שתי לחיצות בו-זמנית על "הזמנה" לא ייצרו כפילות.
- `HasKey(gm => new { gm.GroupId, gm.UserId })` על GroupMembers – מונע גם הצטרפות כפולה כשמאשרים הזמנה פעמיים.
- `HasIndex(u => u.Email).IsUnique()` על Users.
- `HasPrecision(18, 2)` על כל שדה סכום.
- אימייל נשמר תמיד באותיות קטנות (`ToLowerInvariant`), כדי שהזמנה ל-Sara@... תימצא גם כשהמשתמשת נרשמה כ-sara@...
- כל הגדרה בקובץ `IEntityTypeConfiguration` משלה בתיקיית `Configurations`, ו-`ApplyConfigurationsFromAssembly` ב-DbContext.

---

## 6. התחרות על משאבים – התכנון

### איפה התחרות

| משאב | התרחיש | ההגנה |
| --- | --- | --- |
| אחראי אירוע | שני חברים מאשרים "אני האחראי" באותו רגע | `Version` על Events |
| אחראי מקום | שני חברים תופסים את המקום | `Version` על Venues |
| אחראי מתנה | שני חברים תופסים את המתנה | `Version` על Gifts |
| אחראי ארוחה | שני חברים תופסים את הארוחה | `Version` על Meals |
| פריט ברשימת "מה להביא" | שני חברים תופסים "עוגה" או "מפות" באותו רגע | `Version` על EventItems |

המבחן של המורה מתקיים: *"שני משתמשים פעלו בו-זמנית, שניהם קיבלו אישור, ואסור היה ששניהם יקבלו."*

### כלל יסוד: תפיסה היא תמיד עדכון, אף פעם לא יצירה

concurrency token מגן רק על **עדכון** של שורה קיימת. אם שורת ה-Gift הייתה נוצרת רק ברגע שמישהי לוחצת "אני אחראית", שתי לחיצות בו-זמנית היו יוצרות שתי שורות, ושתיהן היו מקבלות אישור.

לכן:
- **Venue ו-Meal נוצרות יחד עם האירוע**, עם `ResponsibleId` ריק.
- **Gift נוצרת כשאחראי האירוע מסמן `HasGift`**, ונמחקת כשהוא מבטל. זו פעולה של אדם אחד, ולכן אין בה תחרות.
- **"תפיסה" = עדכון `ResponsibleId`** בשורה הקיימת, וה-`Version` מגן עליו.
- **אינדקס ייחודי על `EventId`** הוא רשת הביטחון: גם אם משהו ינסה ליצור שורה שנייה, בסיס הנתונים ידחה אותה.

### הזרימה בשכבת ה-Service, בכל תפיסה

1. טעינת הישות **עם tracking** (לא AsNoTracking).
2. בדיקת הרשאה (חבר בקבוצה? Solo/Open?) ← 403.
3. בדיקה עסקית: האחריות או הפריט פנויים? ← 409.
4. השמה, עדכון סטטוס אוטומטי אם צריך, ושמירה.
5. ה-Repository תופס **`DbUpdateConcurrencyException` בלבד** וזורק במקומה `ConcurrencyConflictException` שמוגדרת ב-Core. ה-Service תופס אותה ומחזיר `Result.Conflict` ← 409 עם הודעה ברורה ("המתנה נתפסה על ידי חבר אחר"), ולוג ברמת **Warning**.

**למה התרגום ב-Repository:** `DbUpdateConcurrencyException` שייכת ל-EF Core. אם ה-Service יתפוס אותה ישירות, הוא יצטרך הפניה לחבילת EF Core, וזה שובר את ההפרדה: ה-Service אמור להכיר רק את Core. כך גם בדיקת היחידה פשוטה: ה-Mock של ה-Repository זורק `ConcurrencyConflictException`, בלי שום EF בבדיקה.

הבדיקה העסקית והשמירה קורות באותה פעולה, על אותה ישות שנטענה.

**שלוש טבלאות, קוד אחד:** Venue, Gift ו-Meal חולקות את אותם שדות אחריות (`ResponsibleId`, `Price`, `Version`). כדאי ממשק משותף ב-Core, למשל `IResponsibility`, ומתודת Service אחת לתפיסה ולשחרור – כדי לא לכתוב את אותה לוגיקה שלוש פעמים.

### ההוכחה

בדיקת אינטגרציה: שני DbContext נפרדים מול PostgreSQL אמיתי (בסיס נתונים נפרד לבדיקות, `TandemDb_Test`) קוראים את אותה ישות, שניהם משנים, הראשון שומר בהצלחה והשני מקבל `DbUpdateConcurrencyException`.
**לא** InMemory provider – הוא לא מדמה את xmin, והבדיקה עלולה לעבור גם כשהקוד שבור.

### PostgreSQL – מה שונה ממה שמופיע בשיעורים על SQL Server

| נושא | ב-PostgreSQL |
| --- | --- |
| חבילה | `Npgsql.EntityFrameworkCore.PostgreSQL` ב-Data; ב-Program.cs: `UseNpgsql(...)` |
| Connection string | `Host`, `Port`, `Username`, `Password` – לא `Server`. כולל סיסמה, ולכן **רק ב-User Secrets** |
| Concurrency token | אין `rowversion`. בישות: `public uint Version`. בקובץ ה-Configuration (Fluent API): `builder.Property(e => e.Version).IsRowVersion()`, ו-Npgsql ממפה אותו לעמודת המערכת `xmin`. (במדריך של המורה מופיע `UseXminAsConcurrencyToken()` – זו אותה עמודה ואותו מקום בקוד, אבל בגרסאות החדשות של Npgsql המתודה הזו מסומנת כמיושנת) |
| תאריכים | `DateTime` ממופה ל-`timestamptz`. תאריך שנוצר עם `DateTime.Now` או שה-Kind שלו לא UTC – **זורק חריגה בשמירה**. גם תאריך שמגיע מהלקוח צריך להיות UTC |
| Seed | תאריכים ב-`HasData` חייבים להיות עם `DateTimeKind.Utc` |
| שמות | רגישים לאותיות גדולות וקטנות. שקוף דרך EF Core, אבל בשאילתה ידנית ב-pgAdmin צריך מירכאות: `SELECT * FROM "Events"` |
| הבודקת | צריכה PostgreSQL מותקן ולהגדיר User Secret אחד. את זה מסבירים ב-README; את בסיס הנתונים עצמו המערכת מקימה לבד |

---

## 7. Endpoints

לפי REST: שמות משאבים ברבים, פעולות דרך HTTP methods, בלי פעלים ב-URL.

```
Auth
POST   /api/auth/register                         201
POST   /api/auth/login                            200

Users (Admin)
GET    /api/users?page=&pageSize=                 200
PATCH  /api/users/{id}                            204   חסימה / שחרור

Banks & bank account
GET    /api/banks                                 200   רשימת הבנקים לטופס
GET    /api/me/bank-account                       200
PUT    /api/me/bank-account                       204

Groups
GET    /api/groups                                200   Admin: הכול, Member: שלו
GET    /api/groups/{id}                           200
POST   /api/groups                                201
PUT    /api/groups/{id}                           204   מנהל הקבוצה
DELETE /api/groups/{id}                           204   מנהל הקבוצה / Admin
PUT    /api/groups/{id}/manager                   204   העברת ניהול – מנהל הקבוצה
DELETE /api/groups/{id}/members/{userId}          204   הסרה (מנהל) או עזיבה (החבר עצמו). מנהל שעוזב / חבר עם אחריות פתוחה ← 409
GET    /api/groups/{id}/events?from=&to=          200   לוח שנה

Invitations
POST   /api/groups/{id}/invitations               201   מנהל הקבוצה; גוף: רשימת אימיילים
GET    /api/groups/{id}/invitations               200   מנהל הקבוצה
DELETE /api/invitations/{id}                      204   ביטול – מנהל הקבוצה
GET    /api/me/invitations                        200   ההזמנות הממתינות שלי
PATCH  /api/invitations/{id}                      204   אישור או דחייה – המוזמן; כבר טופלה / פגה ← 409

Events
GET    /api/events?groupId=&status=&search=&from=&to=&sortBy=&sortDir=&page=&pageSize=
GET    /api/events/{id}                           200   כולל מקום, מתנה וארוחה
POST   /api/events                                201   יוצר גם Venue ו-Meal
PUT    /api/events/{id}                           204   אחראי האירוע (כולל HasGift)
DELETE /api/events/{id}                           204   אחראי האירוע; טיוטה – גם היוצר או מנהל הקבוצה
PATCH  /api/events/{id}                           204   ביטול אירוע – אחראי האירוע; גוף: { "status": "Cancelled" }

Responsibilities – המשאב המוגבל
PUT    /api/events/{id}/organizer                 200 / 409
PUT    /api/events/{id}/venue/responsible         200 / 409
DELETE /api/events/{id}/venue/responsible         204
PUT    /api/events/{id}/venue                     204   כתובת ומחיר – אחראי המקום
PUT    /api/events/{id}/gift/responsible          200 / 409
DELETE /api/events/{id}/gift/responsible          204
PUT    /api/events/{id}/gift                      204   תיאור ומחיר – אחראי המתנה
PUT    /api/events/{id}/meal/responsible          200 / 409
DELETE /api/events/{id}/meal/responsible          204
PUT    /api/events/{id}/meal                      204   סוג, קייטרינג ומחיר – אחראי הארוחה

Items – רשימת "מה להביא"
GET    /api/events/{id}/items                     200   פריטים פרטיים מוחזרים רק לאחראי הארוחה
POST   /api/events/{id}/items                     201   אוכל – אחראי הארוחה; כלים – אחראי המקום
DELETE /api/items/{id}                            204   מי שהוסיף את הפריט
PUT    /api/items/{id}/assignee                   200 / 409
DELETE /api/items/{id}/assignee                   204

Messages (תוספת)
GET    /api/events/{id}/messages?page=&pageSize=  200
POST   /api/events/{id}/messages                  201

Settlement
GET    /api/events/{id}/settlement-preview        200   הערכה חיה, לא נשמרת
POST   /api/events/{id}/settlements               201   סגירת חשבון
GET    /api/me/settlements                        200   כמה אני משלם ולמי
PATCH  /api/settlements/{id}                      204   סימון "שילמתי"
```

---

## 8. סדר העבודה ולוח זמנים

יום עבודה = כ-5–6 שעות עבודה מרוכזת.

| # | שלב | ימים | מצב |
| --- | --- | --- | --- |
| 0 | הקמה: Solution, הפניות, GitHub | 1 | ✅ הושלם |
| 1 | תשתית: חבילות, ישויות, DbContext, Migration ראשון, Seed, Middleware, NLog | 2–3 | ⏳ בתהליך |
| 2 | הרשמה, התחברות, JWT, roles | 2 | |
| 3 | קבוצות, הזמנות ואירועים: CRUD, DTOs, AutoMapper, סינון ו-pagination | 4–5 | |
| R1–R2 | ריאקט: הקמה, התחברות, שלד האתר | 2–3 | |
| 4 | **הלב:** תפיסת אחריות (אירוע, מקום, מתנה, ארוחה) ופריטים, concurrency token, 409, סטטוס אוטומטי | 3.5–4.5 | |
| R3–R4 | ריאקט: מסכים, לוח שנה, טופס אירוע, טיפול ב-409 | 3–4 | |
| 5 | בדיקות יחידה | 2 | |
| 6 | בנקים, פרטי חשבון, חישוב תשלומים, Migration שני | 2.5–3 | |
| R5 | ריאקט: מחירים ותשלומים | 1–2 | |
| 7 | סגירת השרת: README, הרצה מאפס | 1 | |
| 8א | תוספת: לוח הודעות באירוע (שרת וריאקט) | 1–1.5 | אחרי הליבה |
| R6 | ליטוש עיצובי | 1–2 | |
| 8ב | תוספת: מיילים ותזכורות | 2.5–3.5 | אופציונלי |

**סך הכול:** כ-26–32 ימי עבודה בלי התוספות (8א, 8ב). מומלץ לתכנן 31–38 ימים, עם מרווח ביטחון של כ-20%.

---

## 9. שלבי השרת – פירוט

### שלב 0: הקמה ✅

- [x] Solution בשם `Tandem` עם `Tandem.API` (תבנית ASP.NET Core Web API, Use controllers, OpenAPI)
- [x] שלושה Class Library: `Tandem.Core`, `Tandem.Data`, `Tandem.Service`
- [x] הפניות: Data → Core; Service → Core; API → Core, Service, Data
- [x] Build: 4 succeeded
- [x] העברה לנתיב `C:\PROJECKT\Tandem`
- [x] Repository `tandem-server` ב-GitHub עם `.gitignore` של Visual Studio

### שלב 1: תשתית (2–3 ימים)

**חבילות ומבנה**
- [x] התקנת PostgreSQL מקומית (installer רשמי ל-Windows, כולל pgAdmin). לרשום את סיסמת המשתמש `postgres` שנבחרת בהתקנה. מותקנת גרסה 18, פורט 5432
- [x] `Npgsql.EntityFrameworkCore.PostgreSQL` (גרסה 9) ב-Data – 9.0.4
- [x] `Microsoft.EntityFrameworkCore.Design` (גרסה 9) ב-API – 9.0.21
- [x] `Swashbuckle.AspNetCore` ב-API – ממשק Swagger. בתבנית של .NET 9 יש רק `AddOpenApi`, בלי ממשק גרפי. גרסה 9.0.6 (במקום `Microsoft.AspNetCore.OpenApi`)
- [x] `dotnet tool install --global dotnet-ef` – 9.0.21
- [ ] מחיקת קבצי הדוגמה של התבנית (WeatherForecast)
- [ ] תיקיות ב-Core: Entities, Enums, Interfaces (Repositories, Services), DTOs, Common, Exceptions
- [ ] שבעת ה-Enums, קובץ לכל אחד

**מודל ובסיס נתונים**
- [ ] תשע ישויות הליבה ב-Core, עם Navigation Properties: Users, Groups, GroupMembers, GroupInvitations, Events, Venues, Gifts, Meals, EventItems (כולל ה-`Version`). Banks, BankAccounts ו-Settlements נוספות בשלב 6 ב-migration שני, ו-EventMessages בשלב 8א ב-migration שלישי
- [ ] `AppDbContext` עם DbSet לכל ישות, והגדרות Fluent API בקבצי Configuration
- [ ] Connection string ב-User Secrets:
  ```
  dotnet user-secrets init --project Tandem.API
  dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=TandemDb;Username=postgres;Password=<הסיסמה שלך>" --project Tandem.API
  ```
- [ ] Seed עם `HasData`: Admin, שני Members, קבוצה (אחד ה-Members מנהל) ואירוע עם Venue ו-Meal. הסיסמאות כ-hash קבוע שחושב מראש, כי `HasData` לא מריץ קוד
- [ ] Migration ראשון:
  ```
  dotnet ef migrations add Init -p Tandem.Data -s Tandem.API
  dotnet ef database update -p Tandem.Data -s Tandem.API
  ```

**תשתית רוחבית**
- [ ] `Result<T>` אחיד ב-Core: Success, NotFound, Forbidden, Conflict, ValidationError
- [ ] `PagedResult<T>`: Items, Page, PageSize, TotalCount
- [ ] ממשק `IEmailSender` ב-Core, ומימוש `LogEmailSender` ב-Data שרק כותב את המייל ללוג (בלי תוכן רגיש). רישום ב-DI. בשלב 8ב מחליפים רק את שורת הרישום
- [ ] `appsettings.json` ו-`appsettings.Development.json`
- [ ] Middleware לטיפול גלובלי בשגיאות: JSON אחיד (ProblemDetails), לוג ברמת Error
- [ ] Middleware ל-CorrelationId: נוצר לכל בקשה, מוחזר ב-header, נכנס ל-logging scope
- [ ] NLog (`NLog.Web.AspNetCore`) עם `nlog.config`; לוג Information לכל בקשה נכנסת, כולל CorrelationId
- [ ] סדר ה-pipeline: ExceptionHandling ← CorrelationId ← CORS ← Authentication ← Authorization ← Controllers. טיפול בשגיאות ראשון, כדרישת המורה. ה-CorrelationId נשמר גם ב-`HttpContext.Items`, וה-ExceptionHandling קורא אותו משם כשהוא כותב את שורת ה-Error – כי ה-logging scope של CorrelationId כבר נסגר כשהחריגה מגיעה אליו

**כללי עבודה מכאן והלאה**
- כל מתודה async מקבלת `CancellationToken` ומעבירה אותו עד ה-DbContext.
- רישום Scoped ל-Repositories ול-Services.

**בדיקת סיום:** מחיקת בסיס הנתונים והרצת `database update` מקימות אותו עם Seed; שגיאה מכוונת מחזירה JSON אחיד ונרשמת בלוג עם CorrelationId.

### שלב 2: הרשמה, התחברות ו-roles (2 ימים)

- [ ] `POST /api/auth/register` ו-`POST /api/auth/login`, עם hashing לסיסמה (`PasswordHasher<T>`)
- [ ] `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] JWT עם claims: מזהה משתמש, אימייל, role
- [ ] מחלקת `JwtSettings` שנטענת עם `IOptions<JwtSettings>`; מפתח ה-JWT ב-User Secrets
- [ ] Swagger עם כפתור Authorize ל-Bearer token
- [ ] HTTP: Location header ב-201, CorrelationId ב-header של כל תשובה, ו-PUT אידמפוטנטי (משתמש שתופס תפקיד שכבר שלו מקבל 200 ולא 409)
- [ ] Endpoints של Admin בלבד: רשימת משתמשים, חסימת משתמש
- [ ] לוגים: אף פעם לא סיסמה, token או גוף בקשת הרשמה

**בדיקת סיום:** בלי token – 401; Member שניגש ל-endpoint של Admin – 403.

### שלב 3: קבוצות, הזמנות ואירועים (4–5 ימים)

- [ ] Repositories ו-Services לקבוצות, חברים ואירועים, עם ממשקים ב-Core
- [ ] `GroupAccessService` לבדיקות הרשאה ברמת קבוצה
- [ ] DTOs נפרדים לבקשה ולתשובה; `AutoMapper` **גרסה 14** עם Profiles (מגרסה 15 נדרש רישיון בתשלום)
- [ ] Data Annotations על DTOs: `[Required]`, `[StringLength]`, `[EmailAddress]`, `[Range]`; ModelState נבדק דרך `[ApiController]`
- [ ] `[FromRoute]`, `[FromQuery]`, `[FromBody]` במפורש בכל פעולה
- [ ] ולידציה עסקית (תאריך עבר, משתמש שאינו חבר) – ב-Service בלבד
- [ ] סטטוסים: 201 עם `CreatedAtAction`, 204 בעדכון ובמחיקה, 404, 400
- [ ] רשימת אירועים עם סינון (קבוצה, סטטוס, טווח תאריכים, חיפוש בכותרת), מיון ו-pagination עם `Skip`/`Take` **בתוך השאילתה**
- [ ] Endpoint ללוח שנה לפי טווח תאריכים
- [ ] `AsNoTracking` בכל שאילתת קריאה; `Include`/`ThenInclude` בעמוד אירוע – בלי N+1
- [ ] הזמנות: יצירה לרשימת אימיילים (מדלגת על חברים קיימים ועל הזמנות ממתינות), תוקף 14 יום, ביטול על ידי המנהל, רשימת "ההזמנות שלי" לפי האימייל של המשתמש המחובר, אישור (מוסיף ל-GroupMembers באותה שמירה) ודחייה
- [ ] יצירת הזמנה "שולחת מייל" דרך `IEmailSender` – בשלב הזה רק שורה בלוג
- [ ] רק מנהל הקבוצה מזמין ומסיר חברים
- [ ] העברת ניהול: רק לחבר בקבוצה; המנהל הקודם נשאר חבר רגיל
- [ ] עזיבת קבוצה: מנהל שמנסה לעזוב בלי להעביר ניהול ← 409 "יש להעביר את ניהול הקבוצה לחבר אחר לפני העזיבה"; מנהל שהוא החבר היחיד יכול רק למחוק את הקבוצה; חבר עם אחריות או פריט באירוע פתוח ← 409 "יש לשחרר את האחריות לפני העזיבה" (גם בהסרה על ידי המנהל)
- [ ] מחיקת טיוטה: היוצר או מנהל הקבוצה; אירוע עם אחראי – רק האחראי
- [ ] ביטול אירוע (`PATCH /api/events/{id}`, סטטוס Cancelled): רק אחראי האירוע; אירוע שכבר Settled ← 409
- [ ] יצירת קבוצה מוסיפה את היוצר גם כמנהל וגם לטבלת GroupMembers, באותה שמירה
- [ ] יצירת אירוע שומרת את `CreatedById`, ויוצרת גם Venue ו-Meal ריקים, באותה שמירה
- [ ] CORS: policy בשם, שה-origins שלה נטענים מהקונפיגורציה (`Cors:AllowedOrigins` ב-appsettings), ולא כתובים בקוד
- [ ] MVC Attributes בכל Controller: `[ApiController]`, `[Route("api/[controller]")]`, `[HttpGet("{id:int}")]` עם route constraints, `[ProducesResponseType]` לכל סטטוס אפשרי (כך Swagger מתעד גם 404 ו-409), `[Authorize]` / `[AllowAnonymous]`
- [ ] Action Filter מותאם אחד לפחות כ-Attribute, למשל `[RequireGroupMember]` שבודק חברות בקבוצה לפני הפעולה

**בדיקת סיום:** אף Controller לא מזריק DbContext; כל endpoint מתועד ב-Swagger.

### שלב 4: הלב – תחרות על משאבים (3.5–4.5 ימים)

- [ ] concurrency token על Events, Venues, Gifts, Meals ו-EventItems:
  ```csharp
  // בישות (Core):
  public uint Version { get; set; }

  // בקובץ ה-Configuration (Data):
  builder.Property(e => e.Version).IsRowVersion();   // Npgsql ממפה אותו לעמודת המערכת xmin
  ```
- [ ] אישור אחראי אירוע
- [ ] ממשק משותף `IResponsibility` ל-Venue, Gift ו-Meal, ולוגיקת תפיסה ושחרור אחת לשלושתם
- [ ] תפיסה ושחרור של אחריות על מקום, מתנה וארוחה
- [ ] עדכון פרטים ומחיר של כל תחום; `[Range(0, 100000)]` על המחיר – 0 תקין
- [ ] `HasGift`: סימון יוצר שורת Gift, ביטול מוחק אותה (רק אחראי האירוע)
- [ ] סוג ארוחה: Shared / Catering / SelfMade. מעבר ל-SelfMade הופך את פריטי האוכל לפרטיים ומשחרר את מי שתפס אותם; מעבר ל-Shared הופך אותם לציבוריים; מעבר ל-Catering חסום כל עוד יש פריטי אוכל ← 409
- [ ] רשימת "מה להביא": הוספה ומחיקה (אוכל – אחראי הארוחה, כלים – אחראי המקום); פריטים פרטיים מסוננים בשאילתה ולא מוחזרים לאחרים
- [ ] תפיסה ושחרור של פריט ציבורי – פתוח לכל חבר, גם ב-Solo
- [ ] כללי Solo ו-Open לתחומי האחריות
- [ ] `ConcurrencyConflictException` ב-Core; ה-Repository מתרגם אליה את `DbUpdateConcurrencyException` (ורק אותה); ה-Service מחזיר Conflict ← 409, לוג Warning
- [ ] מעבר אוטומטי Organizing ↔ Organized אחרי כל תפיסה, שחרור או עדכון: אחראי אירוע + מקום וארוחה (ומתנה אם `HasGift`) עם אחראי ומחיר שהוזן + כל הפריטים הציבוריים נתפסו
- [ ] פרויקט `Tandem.Tests`, ובו בדיקת האינטגרציה עם שני DbContext מול PostgreSQL

**בדיקת סיום:** שני חלונות Swagger שתופסים את אותה אחריות (למשל המתנה): אחד מקבל 200, השני 409, ובלוג מופיעה שורת Warning.

### שלב 5: בדיקות יחידה (2 ימים)

- [ ] xUnit ו-Moq על שכבת ה-Service בלבד, עם Repositories מדומים
- [ ] **חובה:** תפיסת משאב – מקרה תקין, מקרה שנדחה כי התפקיד כבר תפוס, ומקרה שנדחה כי ה-Repository המדומה זורק `ConcurrencyConflictException` (מדמה התנגשות)
- [ ] Solo מול Open; משתמש שאינו חבר בקבוצה; חבר רגיל שמנסה להזמין; מנהל שמנסה לעזוב בלי להעביר ניהול; חבר עם אחריות פתוחה שמנסה לעזוב
- [ ] הזמנות: אישור מוסיף לקבוצה; הזמנה שפגה נדחית; הזמנה שכבר טופלה נדחית; משתמש שמנסה לאשר הזמנה של אימייל אחר
- [ ] פריט פרטי לא מוחזר למי שאינו אחראי הארוחה
- [ ] מעבר סטטוס אוטומטי: מקרה שמשלים ארגון, מקרה שחסר בו משהו, אירוע בלי מתנה, ומחיר 0 שנחשב "הוזן"

### שלב 6: בנקים, פרטי חשבון וחישוב תשלומים (2.5–3 ימים)

- [ ] ישויות Banks, BankAccounts ו-Settlements, Seed של רשימת בנקים, ו-Migration שני: `AddBanksAndSettlements` (חובה – זו ה-migration השנייה שהמורה דורשת)
- [ ] Endpoints לבנקים, פרטי חשבון, סגירת חשבון וסימון תשלום
- [ ] `SettlementCalculator` כמחלקה נפרדת, בלי תלות בבסיס הנתונים:
  - הקלט: רשימת חברי הקבוצה, ורשימת (משלם, סכום) מהמקום, המתנה והארוחה
  - יתרה לכל חבר = מה ששילם − החלק שלו
  - התאמה בין חייבים (יתרה שלילית) לזכאים (יתרה חיובית)
  - OrganizerPays: כל הסכום על אחראי האירוע; מי ששילם על תחום מקבל ממנו בחזרה
  - תחום עם מחיר 0, או מתנה שלא קיימת – לא נכנס לחישוב
- [ ] עיגול אגורות: השארית מתחלקת באגורה לחברים הראשונים, כך שהסכום מתאזן תמיד
- [ ] טעינת האירוע עם המקום, המתנה, הארוחה וחברי הקבוצה **בשאילתה אחת** (`Include`) לפני החישוב
- [ ] תצוגה חיה (`settlement-preview`) – מחשבת בלי לשמור
- [ ] סגירת חשבון רק כשהסטטוס Organized; יוצרת Settlements, נועלת מחירים, מעבירה ל-Settled. שינוי מחיר אחר כך ← 409
- [ ] פרטי חשבון גלויים רק למי שחייב כסף לבעל החשבון
- [ ] בדיקות: חלוקה שווה, OrganizerPays, כמה משלמים, סכום שלא מתחלק שווה, אירוע בלי מתנה, מחירים של 0

### שלב 7: סגירת השרת (יום אחד)

- [ ] `Database.Migrate()` בעליית השרת בסביבת Development – הבודקת לא מריצה שום פקודה
- [ ] README: תיאור המערכת, המשאב המוגבל ואופן הטיפול בתחרות, הוראות הרצה: התקנת PostgreSQL והגדרת ה-connection string ב-User Secrets, משתמשי דמו לכל role עם פרטי כניסה
- [ ] מעבר על רשימת הבדיקה (סעיף 12), סעיף אחר סעיף
- [ ] `git clone` לתיקייה חדשה, הרצה מאפס – ועובד

---

## 10. מסלול הריאקט – פירוט

מתחיל אחרי שלב 3 בשרת. כל מסך נבנה רק אחרי שה-endpoint שלו נבדק ב-Swagger.

### R1: הקמה ותשתית

- [ ] Repository `tandem-client` ב-GitHub
- [ ] `npm create vite@latest tandem-client -- --template react-ts`
- [ ] ספריית רכיבים (MUI או Tailwind) – ההחלטה העיצובית היחידה בשלב זה
- [ ] מבנה תיקיות: `api`, `features/auth`, `features/groups`, `features/events`, `components`, `hooks`, `types`
- [ ] מופע axios אחד עם interceptor: מוסיף JWT, מטפל ב-401
- [ ] טיפוסי TypeScript תואמים ל-DTOs של השרת

### R2: אימות והרשאות

- [ ] Redux Toolkit: slice למשתמש המחובר ול-token בלבד
- [ ] Context: מערכת התראות (toast) – כך שגם Redux וגם Context בשימוש
- [ ] מסכי התחברות והרשמה עם React Hook Form ו-validation
- [ ] `ProtectedRoute` למשתמש מחובר, `AdminRoute` לפי role
- [ ] שלד האתר: כותרת, ניווט, פריסה

### R3: נתונים ומסכים

- [ ] React Query לכל נתוני השרת, דרך hooks ייעודיים (`useGroups`, `useEvents`, `useClaimItem`). נתוני שרת לא נשמרים ב-Redux
- [ ] רשימת קבוצות, ובראשה ההזמנות הממתינות עם אישור ודחייה
- [ ] עמוד קבוצה: חברים; למנהל בלבד – הזמנת חברים (טופס עם כמה אימיילים), רשימת הזמנות ומצבן, הסרת חבר והעברת ניהול; כפתור "עזיבת קבוצה" עם הודעה מתאימה כשאי אפשר לעזוב
- [ ] לוח שנה של קבוצה
- [ ] רשימת אירועים עם חיפוש, סינון, מיון ו-pagination מול השרת
- [ ] טופס אירוע **אחד** ליצירה ולעריכה, עם שדות בחירה ו-validation
- [ ] עמוד אירוע: אחראי אירוע, כרטיס לכל תחום (מקום, מתנה אם יש, ארוחה) עם כפתור תפיסה וטופס פרטים ומחיר, ורשימת "מה להביא" בשני חלקים – אוכל וכלים ועריכה; לאחראי הארוחה ב-"מכין לבד" – התפריט הפרטי שלו
- [ ] מסך Admin: כל המשתמשים וכל הקבוצות

### R4: טיפול ב-409 – המסך הקריטי לשתי המורות

- [ ] הודעה ברורה כשהשרת מחזיר 409
- [ ] רענון אוטומטי עם `invalidateQueries`, כך שהמשתמש רואה מי תפס

### R5: מחירים ותשלומים

- [ ] תצוגה חיה של החלוקה המשוערת, כפתור סגירת חשבון לאחראי האירוע
- [ ] מסך "כמה אני משלם ולמי", עם פרטי החשבון וכפתור "שילמתי"
- [ ] עמוד פרטי חשבון בנק בפרופיל, עם רשימת בנקים נפתחת

### R6: ליטוש עיצובי – רק אחרי שהכול עובד

- [ ] theme אחד לצבעים, ריווח וטיפוגרפיה
- [ ] עיצוב לוח השנה ועמוד האירוע
- [ ] התאמה למסך טלפון

### כללים לאורך כל המסלול

- רכיבי `Loading` ו-`ErrorMessage` משותפים, בשימוש בכל מסך
- אין קומפוננטה מעל כ-150 שורות; מה שגדל – מתפצל
- State מקומי רק למה ששייך לקומפוננטה; נתונים עוברים ב-props, ולא ב-Context כשאין צורך

---

## 11. שלב 8: תוספות (רק אחרי שכל רשימת הבדיקה מסומנת)

### 8א: לוח הודעות באירוע (1–1.5 ימים)

- [ ] ישות EventMessages ו-Migration שלישי: `AddEventMessages`
- [ ] `GET` עם pagination (החדשות ראשונות) ו-`POST`; רק חברי הקבוצה; `[StringLength(500)]` על הטקסט
- [ ] ריאקט: רשימת הודעות בעמוד האירוע ותיבת כתיבה; `refetchInterval` של React Query מרענן כל כמה שניות
- [ ] **לא SignalR** – המורה כתבה שהוא לא נדרש, והרענון התקופתי מספיק

### 8ב: מיילים ותזכורות (2.5–3.5 ימים)

- [ ] `SmtpEmailSender` ב-Data, עם `SmtpSettings` דרך `IOptions`; סיסמת SMTP ב-User Secrets. מחליפים רק את שורת הרישום ב-Program.cs – שום Service לא משתנה
- [ ] לבדיקות ולהדגמה: **smtp4dev** – שרת SMTP מקומי שתופס את המיילים ומציג אותם, בלי לשלוח באמת. (ייתכן גם שסינון האינטרנט חוסם שליחה אמיתית מהמחשב)
- [ ] מייל הזמנה לקבוצה, עם קישור למערכת
- [ ] `BackgroundService` שרץ פעם ביום:
  - שבוע לפני אירוע שלא אורגן – התראה לאחראי האירוע ולאחראי התחומים
  - יומיים לפני – תזכורת לכל החברים עם המיקום
  - כמה ימים אחרי האירוע – תזכורת לאחראי לסגור את החשבון
- [ ] מייל לכל החברים כשהאירוע עובר ל-Organized, ובסגירת החשבון
- [ ] עמודה `ReminderSentAt` או טבלת לוג קטנה, כדי שתזכורת לא תישלח פעמיים
- [ ] `BackgroundService` הוא Singleton – הוא יוצר scope חדש (`IServiceScopeFactory`) בכל ריצה כדי לקבל DbContext. בדיוק הדוגמה של "Singleton שמחזיק Scoped" מסעיף 3

---

## 12. רשימת בדיקה מול הדרישות

עוברים על הטבלאות בסוף כל שלב ולפני ההגשה.

### דרישות דוטנט

| | דרישה | שלב |
| --- | --- | --- |
| ☐ | 1. REST, סטטוסים 200/201+Location/204/400/401/403/404/409, Swagger | 2, 3, 4 |
| ☐ | 2. FromRoute/FromQuery/FromBody, Data Annotations, ModelState, ולידציה עסקית ב-Service | 3 |
| ☐ | 3. ארבעה פרויקטים, כיוון תלויות נכון, Core נקי | 0 |
| ☐ | 4. DI ב-constructor, lifetimes מודעים, הסבר Singleton מול Scoped | 1 |
| ☐ | 5. Code-First, DbSet לכל ישות, שתי migrations לפחות, connection string מהקונפיגורציה, Seed | 1, 4/6 |
| ☐ | 6. CRUD מלא, LINQ עם סינון ומיון, Skip/Take בשאילתה, AsNoTracking | 3 |
| ☐ | 7. one-to-many, many-to-many, Include/ThenInclude, Fluent API, בלי N+1 | 1, 3, 6 |
| ☐ | 5א. שלוש migrations: Init, AddBanksAndSettlements, AddEventMessages | 1, 6, 8א |
| ☐ | 8. DTOs נפרדים ל-request ול-response, AutoMapper Profiles | 3 |
| ☐ | 9. async מקצה לקצה, CancellationToken | 1 והלאה |
| ☐ | 10. Middleware לשגיאות ול-CorrelationId, סדר pipeline נכון | 1 |
| ☐ | 11. appsettings לפי סביבה, IOptions, User Secrets, אין סודות ב-repo | 1, 2 |
| ☐ | 12. JWT, הרשמה והתחברות, שני roles עם הרשאות שונות בפועל, [Authorize] | 2 |
| ☐ | 13. xUnit + Moq על Service, בדיקת משאב מוגבל – תקין ונדחה | 5 |
| ☐ | חלק ג: concurrency token, 409, בדיקה עם שני DbContext, הסבר ב-README | 4, 7 |
| ☐ | חלק ד: NLog, CorrelationId בלוג, Error לחריגות, Warning להתנגשויות, בלי סודות | 1, 2, 4 |
| ☐ | חלק ה: התחברות, רשימה עם pagination, פעולה על המשאב, הצגת 409 | R2–R4 |
| ☐ | חלק ו: שני repositories, gitignore, README, עלייה בלי התערבות ידנית | 0, 7 |

### נושאי הסילבוס

| נושא | איפה בפרויקט | שלב |
| --- | --- | --- |
| HTTP Protocol | methods לפי משמעות, סטטוסים נכונים, headers (Authorization, Location, X-Correlation-Id), אידמפוטנטיות של PUT ו-DELETE | 2, 3, 4 |
| RESTful | שמות משאבים ברבים, משאבים מקוננים (`/events/{id}/items`), בלי פעלים ב-URL | 3 (סעיף 7) |
| Clean Architecture | ארבע שכבות, תלויות פנימה בלבד, Core נקי מתשתיות, ממשקים ב-Core ומימושים בחוץ | 0, 1 |
| Web API | Controllers, `[ApiController]`, ProblemDetails, Swagger | 0–3 |
| Dependency Injection | הזרקה ב-constructor, Scoped/Transient/Singleton במודע, רישום מרוכז ב-Program.cs | 1 |
| Entity Framework | Code-First, Fluent API, migrations, Seed, Include, AsNoTracking, concurrency token | 1, 3, 4 |
| Asynchronous programming | async מקצה לקצה, CancellationToken, בלי `.Result` או `.Wait()` | 1 והלאה |
| AutoMapper | Profiles ב-Service, מיפוי Entity ↔ DTO בלבד | 3 |
| Middlewares | טיפול גלובלי בשגיאות, CorrelationId, סדר pipeline | 1 |
| Cors | policy בשם, origins מהקונפיגורציה | 3 |
| Configuration | appsettings לפי סביבה, `IOptions<JwtSettings>`, User Secrets | 1, 2 |
| MVC Attributes | Route, Http*, ProducesResponseType, Authorize, From*, Action Filter מותאם | 3 |
| Model Binding | FromRoute, FromQuery (אובייקט פרמטרים לסינון ו-pagination), FromBody, Data Annotations | 3 |
| Logging | ILogger בקוד, NLog מתחת, רמות נכונות, CorrelationId, בלי מידע רגיש | 1, 2, 4 |

### דרישות ריאקט

| | דרישה | איפה |
| --- | --- | --- |
| ☐ | כניסה למערכת | R2 |
| ☐ | שני סוגי משתמשים עם הרשאות שונות | R2 (AdminRoute), R3 (מסך Admin, ניהול חברים למנהל) |
| ☐ | אובייקט מרכזי: רשימה, הוספה ועריכה | R3 – Event |
| ☐ | לפחות 5 שדות משמעותיים | Event: כותרת, תאריך, אופן ארגון, יש מתנה, שיטת חלוקה, סטטוס |
| ☐ | שדה בחירה | סטטוס, סוג ארוחה, Solo/Open, שיטת חלוקה, סוג פריט, בנק |
| ☐ | חיפוש, סינון או מיון | R3, מול השרת |
| ☐ | נתונים משרת ומסד נתונים | כל המסלול |
| ☐ | מנהל רואה הכול, משתמש רואה את שלו (ואת הציבוריות) | Admin – כל הקבוצות והאירועים; Member – רק הקבוצות שהוא חבר בהן. "רשומות ציבוריות" – ראו שאלות פתוחות |
| ☐ | React + TypeScript, React Router, React Hook Form, React Query | R1–R3 |
| ☐ | Redux Toolkit / Context | R2 – שניהם |
| ☐ | חלוקה לקומפוננטות, Hooks, Routing, State ו-Props | R1, R3, כללים |
| ☐ | טפסים עם Validation | R2, R3 |
| ☐ | קריאות מסודרות לשרת | R1 (axios אחד), R3 (hooks ייעודיים) |
| ☐ | Loading, טיפול בשגיאות, מניעת שכפול קוד | כללים, R4 |
| ☐ | אין קומפוננטה אחת גדולה | כללים |

### שאלות פתוחות

- למורת הריאקט: בדרישות מופיעה דוגמה שמשתמש רגיל רואה גם "רשומות ציבוריות". ב-Tandem כל המידע שייך לקבוצה, ולכן אין כרגע רשומות ציבוריות. אם היא מצפה לזה, הפתרון הקל: שדה `IsPublic` על Group, וקבוצות ציבוריות מוצגות לכל המשתמשים לצפייה בלבד ברשימת "קבוצות ציבוריות". זה שדה אחד, תנאי אחד בשאילתה ומסך אחד – בלי לגעת בשאר המערכת.

- לשתי המורות: האם מותר להגיש את אותה מערכת לשני הקורסים?
- למורת הריאקט: האם "Redux Toolkit Context" פירושו אחד מהשניים או שניהם? (בינתיים מתוכננים שניהם.)

---

## 13. כללי עבודה

### Git

- **מתי commit:** אחרי כל צעד קטן שעובד ועובר Build – בערך כל שעה–שעתיים, או אחרי כל סעיף בתוכנית.
- **מתי לא:** כשה-Build נכשל, או באמצע שינוי.
- **איך:** חלון **Git Changes** (View ← Git Changes, או האייקון בשורת המצב) ← הודעה קצרה באנגלית ← **Commit All** ← **Push**.
- **הודעות טובות:** `Add enums`, `Create Event entity`, `Add JWT authentication`, `Fix concurrency check on event items`.

### פיתוח

- **Swagger הוא החוזה:** endpoint נבדק בו לפני שנבנה עבורו מסך.
- **Controllers רזים:** קבלת בקשה, קריאה ל-Service, תרגום `Result` לסטטוס. בלי לוגיקה.
- **לוגים ושגיאות מהיום הראשון** – הם הכלי לדבג את כל השאר.
- **תאריכים ב-UTC** לאורך כל המערכת, תמיד `DateTime.UtcNow`; המרה לשעון מקומי רק בתצוגה. ב-PostgreSQL זו חובה, לא המלצה (ראו סעיף 6).
- **תכונה שגוזלת פי שניים מהמתוכנן:** עוצרים ובודקים אם היא חיונית לדרישות.
- **לפני הגשה:** `git clone` לתיקייה חדשה והרצה מאפס – בדיוק כמו שהבודקת תעשה.
