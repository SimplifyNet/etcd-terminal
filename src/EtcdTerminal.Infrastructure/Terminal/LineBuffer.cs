using System.Text;

namespace EtcdTerminal.Infrastructure.Terminal;

/// <summary>
/// The text of a prompt answer and the position of the caret inside it. The
/// buffer knows the editing keys only; it draws nothing and reads nothing.
/// </summary>
public sealed class LineBuffer(string initial = "")
{
	private readonly StringBuilder _text = new(initial);

	public string Text => _text.ToString();

	public int Cursor { get; private set; } = initial.Length;

	public int Length => _text.Length;

	/// <summary>
	/// Applies an editing key. Returns <c>false</c> for keys the buffer does
	/// not understand, so the caller can treat them as its own commands.
	/// </summary>
	public bool Apply(ConsoleKeyInfo key)
	{
		var control = key.Modifiers.HasFlag(ConsoleModifiers.Control);

		switch (key.Key)
		{
			case ConsoleKey.LeftArrow when control:
				Cursor = WordStart(Cursor);
				return true;
			case ConsoleKey.RightArrow when control:
				Cursor = WordEnd(Cursor);
				return true;
			case ConsoleKey.LeftArrow:
				Cursor = Math.Max(0, Cursor - 1);
				return true;
			case ConsoleKey.RightArrow:
				Cursor = Math.Min(_text.Length, Cursor + 1);
				return true;
			case ConsoleKey.Home:
				Cursor = 0;
				return true;
			case ConsoleKey.End:
				Cursor = _text.Length;
				return true;
			case ConsoleKey.Backspace when control:
				Remove(WordStart(Cursor), Cursor);
				return true;
			case ConsoleKey.Backspace:
				Remove(Cursor - 1, Cursor);
				return true;
			case ConsoleKey.Delete when control:
				Remove(Cursor, WordEnd(Cursor));
				return true;
			case ConsoleKey.Delete:
				Remove(Cursor, Cursor + 1);
				return true;
		}

		if (key.KeyChar is '\0' || char.IsControl(key.KeyChar) || control || key.Modifiers.HasFlag(ConsoleModifiers.Alt))
			return false;

		_text.Insert(Cursor, key.KeyChar);
		Cursor++;

		return true;
	}

	private void Remove(int from, int to)
	{
		from = Math.Max(0, from);
		to = Math.Min(_text.Length, to);

		if (from >= to)
			return;

		_text.Remove(from, to - from);
		Cursor = from;
	}

	private int WordStart(int position)
	{
		while (position > 0 && char.IsWhiteSpace(_text[position - 1]))
			position--;

		while (position > 0 && !char.IsWhiteSpace(_text[position - 1]))
			position--;

		return position;
	}

	private int WordEnd(int position)
	{
		while (position < _text.Length && !char.IsWhiteSpace(_text[position]))
			position++;

		while (position < _text.Length && char.IsWhiteSpace(_text[position]))
			position++;

		return position;
	}
}
