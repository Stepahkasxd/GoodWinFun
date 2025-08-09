using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace GoodWin.Gui.Validation;

public class KeyValidationRule : ValidationRule
{
    private static readonly HashSet<string> Special = new(StringComparer.OrdinalIgnoreCase)
    {
        "SPACE","RETURN","ESCAPE","TAB","BACKSPACE","OEM3",
        "UPARROW","DOWNARROW","LEFTARROW","RIGHTARROW",
        "MOUSE1","MOUSE2","MWHEELUP","MWHEELDOWN"
    };

    public override ValidationResult Validate(object value, CultureInfo culture)
    {
        var text = (value as string)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return ValidationResult.ValidResult;
        if (text.Length == 1 && char.IsLetterOrDigit(text[0])) return ValidationResult.ValidResult;
        if (Special.Contains(text.ToUpperInvariant())) return ValidationResult.ValidResult;
        return new ValidationResult(false, "Недопустимая клавиша");
    }
}
