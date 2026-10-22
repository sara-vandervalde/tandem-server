using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Banks;

// Body of PUT /api/me/bank-account: creates the account the first time, replaces it afterwards
public class SaveBankAccountRequest
{
    [Required(ErrorMessage = "יש לבחור בנק")]
    [Range(1, int.MaxValue, ErrorMessage = "מזהה בנק לא תקין")]
    public int? BankId { get; init; }

    // Digits only, kept as text so a leading 0 is not lost
    [Required(ErrorMessage = "יש להזין מספר סניף")]
    [RegularExpression(@"^\d{1,5}$", ErrorMessage = "מספר סניף – ספרות בלבד, עד 5")]
    public string BranchNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "יש להזין מספר חשבון")]
    [RegularExpression(@"^\d{2,20}$", ErrorMessage = "מספר חשבון – ספרות בלבד, בין 2 ל-20")]
    public string AccountNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "יש להזין את שם בעל/ת החשבון")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "השם צריך להכיל בין 2 ל-100 תווים")]
    public string AccountHolderName { get; init; } = string.Empty;
}
