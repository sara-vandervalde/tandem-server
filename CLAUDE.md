# CLAUDE.md – הנחיות עבודה לפרויקט Tandem

## הקשר

- זהו פרויקט סיום של סטודנטית בקורס .NET Web API (השרת) ובקורס React (הלקוח, ב-repository נפרד).
- **התוכנית המלאה נמצאת ב-`PLAN.md`.** קרא אותה בתחילת כל שיחה, לפני כל צעד.
- **`USER_FLOW.md` מתאר איך המשתמשות עובדות עם המערכת**, וההחלטות שסוכמו מופיעות בסופו. כשיש ספק איך תכונה צריכה להתנהג – בדוק שם. אם הקוד או `PLAN.md` סותרים אותו, עצור ושאל.
- דרישות המורות ממופות בסעיף 12 של `PLAN.md`. כל החלטה צריכה לעמוד בהן.

## אופן העבודה

1. **שלב אחרי שלב.** עובדים על סעיף אחד מתוך `PLAN.md` בכל פעם, לפי הסדר. לא מדלגים קדימה ולא בונים כמה שלבים יחד.
2. **בתחילת כל צעד:** אמור באיזה שלב וסעיף אנחנו, מה נעשה ולמה, ואיזו דרישה של המורה הוא מכסה.
3. **הסבר את הקוד.** הסטודנטית צריכה להבין ולהציג כל שורה בהגנה על הפרויקט. אחרי כל קטע קוד – הסבר קצר של מה הוא עושה ולמה בחרנו כך. מושג חדש (למשל concurrency token, middleware) – הסבר אותו לפני שמשתמשים בו.
4. **צעדים קטנים.** קובץ אחד או שינוי ממוקד אחד בכל פעם, ואז עצירה לבדיקה.
5. **בסוף כל צעד:** איך לבדוק שהוא עובד (Build, Swagger, הרצת בדיקות), והצעה להודעת commit קצרה באנגלית.
6. **עדכון התוכנית:** אחרי שהסטודנטית מאשרת שצעד עובד, סמן את הסעיף ב-`PLAN.md` כ-`[x]` ועדכן את עמודת "מצב" בטבלת סדר העבודה.
7. **החלטה שמשנה את התוכנית** (טבלה חדשה, שינוי endpoint, דילוג על סעיף) – שאל קודם, ואחרי אישור עדכן את `PLAN.md`.
8. **תקשורת דרך קבצים:** הסטודנטית כותבת ב-`ask.txt`, והתשובה המלאה נכתבת ב-`reply.txt` – בשפה פשוטה, עם הסבר לכל מונח. בצ'אט – שורה או שתיים בלבד.

## שפה

- הסברים ושיחה – בעברית.
- קוד, שמות קבצים, מחלקות, משתנים והודעות commit – באנגלית.
- הודעות שגיאה שמוחזרות למשתמש מה-API – בעברית.

## כללים טכניים שאסור לשבור

- **ארכיטקטורה:** `Tandem.Core` לא מפנה לשום פרויקט ולשום חבילה חיצונית. Data ו-Service מפנים ל-Core בלבד. API מכיר את Data רק לרישום ב-Program.cs. Controller לעולם לא מזריק DbContext.
- **בסיס נתונים:** PostgreSQL עם Npgsql. concurrency token: `public uint Version` בישות (בלי attribute), ובקובץ ה-Configuration: `builder.Property(e => e.Version).IsRowVersion();` – Npgsql ממפה אותו ל-`xmin`. כל `DateTime` ב-UTC (`DateTime.UtcNow`) – אחרת השמירה נכשלת.
- **תחרות:** ה-Repository מתרגם `DbUpdateConcurrencyException` ל-`ConcurrencyConflictException` מ-Core; ה-Service מחזיר Conflict (409) ורושם Warning. לא תופסים `Exception` כללי.
- **async:** כל שרשרת הקריאה async, ו-`CancellationToken` מועבר עד ה-DbContext.
- **שאילתות:** `AsNoTracking` בקריאה בלבד, `Skip`/`Take` בתוך השאילתה, `Include` במקום לולאות (בלי N+1).
- **DTOs:** ישויות לא יוצאות ולא נכנסות דרך ה-API; מיפוי עם AutoMapper.
- **סודות:** connection string ומפתח JWT רק ב-User Secrets. אף סוד לא נכנס לקוד, ל-appsettings.json או ל-repository.
- **לוגים:** ILogger בלבד בקוד (NLog מתחת). לעולם לא לרשום סיסמאות, tokens, גוף בקשת הרשמה או פרטי חשבון בנק.

## סביבה

- Windows, Visual Studio, .NET 9.
- מיקום: `C:\PROJECKT\Tandem`, Solution בשם `Tandem` עם `Tandem.Core`, `Tandem.Data`, `Tandem.Service`, `Tandem.API`.
- Repository: `tandem-server` ב-GitHub.
- פקודות migrations מריצים משורש ה-solution:
  ```
  dotnet ef migrations add <Name> -p Tandem.Data -s Tandem.API
  dotnet ef database update -p Tandem.Data -s Tandem.API
  ```
