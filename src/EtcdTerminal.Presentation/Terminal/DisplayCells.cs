using System.Buffers;
using System.Globalization;
using System.Text;

namespace EtcdTerminal.Terminal;

/// <summary>
/// Display-cell measurement shared by production terminals and layout tests.
/// East Asian wide/fullwidth runes occupy two cells, combining marks and
/// control characters occupy none, everything else occupies one cell.
/// Grapheme clusters (ZWJ sequences, flags) are not combined.
/// </summary>
public static class DisplayCells
{
	public static int Width(string text)
	{
		var width = 0;
		var span = text.AsSpan();

		while (!span.IsEmpty)
		{
			if (Rune.DecodeFromUtf16(span, out var rune, out var consumed) is not OperationStatus.Done)
			{
				width++;

				span = span[1..];
				continue;
			}

			width += Width(rune);

			span = span[consumed..];
		}

		return width;
	}

	public static int Width(Rune rune) => rune.Value switch
	{
		< 0x20 or >= 0x7F and <= 0x9F => 0,
		_ when IsCombining(rune.Value) => 0,
		_ when IsWide(rune.Value) => 2,
		_ => 1
	};

	private static bool IsCombining(int value) =>
		Rune.GetUnicodeCategory((Rune)value) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark;

	private static bool IsWide(int value) => value switch
	{
		>= 0x1100 and <= 0x115F => true,
		>= 0x231A and <= 0x231B => true,
		>= 0x2329 and <= 0x232A => true,
		>= 0x23E9 and <= 0x23EC => true,
		0x23F0 or 0x23F3 => true,
		>= 0x25FD and <= 0x25FE => true,
		>= 0x2614 and <= 0x2615 => true,
		0x2640 or 0x2642 => true,
		>= 0x2699 and <= 0x269C => true,
		0x26A1 or >= 0x26AA and <= 0x26AB => true,
		>= 0x26BD and <= 0x26BE => true,
		>= 0x26C4 and <= 0x26C5 => true,
		0x26CE or 0x26D4 or 0x26EA => true,
		>= 0x26F2 and <= 0x26F3 => true,
		0x26F5 or 0x26FA or 0x26FD => true,
		0x2705 or >= 0x270A and <= 0x270B => true,
		0x2728 or 0x274C or 0x274E => true,
		>= 0x2753 and <= 0x2755 => true,
		0x2757 or >= 0x2795 and <= 0x2797 => true,
		0x27B0 or 0x27BF => true,
		>= 0x2B1B and <= 0x2B1C => true,
		0x2B50 or 0x2B55 => true,
		>= 0x3000 and <= 0x303E => true,
		>= 0x3041 and <= 0x33FF => true,
		>= 0x3400 and <= 0x4DBF => true,
		>= 0x4E00 and <= 0xA4C6 => true,
		>= 0xA960 and <= 0xA97C => true,
		>= 0xAC00 and <= 0xD7A3 => true,
		>= 0xF900 and <= 0xFAFF => true,
		>= 0xFE10 and <= 0xFE19 => true,
		>= 0xFE30 and <= 0xFE52 => true,
		>= 0xFE54 and <= 0xFE66 => true,
		>= 0xFF00 and <= 0xFF60 => true,
		>= 0xFFE0 and <= 0xFFE6 => true,
		0x1F004 or 0x1F0CF or 0x1F18E => true,
		>= 0x1F191 and <= 0x1F19A => true,
		>= 0x1F200 and <= 0x1F202 => true,
		>= 0x1F210 and <= 0x1F23B => true,
		>= 0x1F240 and <= 0x1F248 => true,
		0x1F250 or 0x1F251 => true,
		>= 0x1F300 and <= 0x1F64F => true,
		>= 0x1F680 and <= 0x1F6FF => true,
		>= 0x1F900 and <= 0x1F9FF => true,
		>= 0x20000 and <= 0x3FFFD => true,
		_ => false
	};
}
