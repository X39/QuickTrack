using System.Text;
using TextCopy;
using X39.Util.Collections;

namespace QuickTrack;

public class InteractiveConsoleInput
{
    private class InputLineHandler
    {
        private class Cursor
        {
            private readonly StringBuilder _buffer;
            private readonly bool _moveConsole;

            public Cursor(StringBuilder buffer, bool moveConsole = false)
            {
                _buffer = buffer;
                _moveConsole = moveConsole;
            }

            public int Index { get; private set; }

            public bool MoveTo(int index)
            {
                if (index < 0) index = 0;
                if (index > _buffer.Length) index = _buffer.Length;
                var delta = index - Index;
                switch (delta)
                {
                    case < 0:
                    {
                        if (_moveConsole)
                        {
                            var str = new string('\b', -delta);
                            Console.Write(str);
                        }

                        Index = index;
                        return true;
                    }
                    case > 0:
                    {
                        if (_moveConsole)
                        {
                            while (delta-- > 0)
                            {
                                Console.Write(_buffer[Index]);
                                Index++;
                            }
                        }
                        Index = index;
                        return true;
                    }
                    case 0:
                        return false;
                }
            }
            public bool CanMoveTo(int index)
            {
                if (index < 0) return false;
                if (index > _buffer.Length) return false;
                var delta = index - Index;
                return delta switch
                {
                    < 0 => true,
                    > 0 => true,
                    0 => false
                };
            }

            public bool Move(int delta)
                => MoveTo(Index - delta);

            public bool MoveLeft()
                => MoveTo(Index - 1);

            public bool MoveLeft(int index)
                => MoveTo(Index - index);

            public bool MoveRight()
                => MoveTo(Index + 1);

            public bool MoveRight(int index)
                => MoveTo(Index + index);

            public bool CanMove(int delta)
                => CanMoveTo(Index - delta);

            public bool CanMoveLeft()
                => CanMoveTo(Index - 1);

            public bool CanMoveLeft(int index)
                => CanMoveTo(Index - index);

            public bool CanMoveRight()
                => CanMoveTo(Index + 1);

            public bool CanMoveRight(int index)
                => CanMoveTo(Index + index);
        }

        private readonly StringBuilder _builder;
        private readonly Cursor _cursor;
        private readonly Stack<string> _undo;
        private readonly Stack<string> _redo;

        public InputLineHandler(Stack<string> undo, Stack<string> redo)
        {
            _builder = new StringBuilder();
            _cursor = new Cursor(_builder, true);
            _undo = undo;
            _redo = redo;
        }

        public int Length => _builder.Length;
        
        private bool RemoveLeft()
        {
            if (!_cursor.MoveLeft()) return false;
            _builder.Remove(_cursor.Index, 1);
            Rewrite(1);
            return true;
        }

        private void RemoveLeft(int count)
        {
            while (count-- > 0 && RemoveLeft())
            {
                // empty;
            }
        }

        private bool RemoveRight()
        {
            if (!_cursor.CanMoveRight()) return false;
            _builder.Remove(_cursor.Index, 1);
            Rewrite(1);
            return true;
        }

        private void RemoveRight(int count)
        {
            while (count-- > 0 && RemoveRight())
            {
                // empty;
            }
        }

        public string ReadLine(CancellationToken cancellationToken)
        {
            Console.Write(">");

            while (!cancellationToken.IsCancellationRequested)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    {
                        if (!_undo.Any())
                        {
                            Console.Beep();
                            continue;
                        }

                        if (Length > 0)
                        {
                            _cursor.MoveTo(0);
                            _redo.Push(_builder.ToString());
                        }

                        var previous = _undo.Pop();
                        var length = Length;
                        _builder.Clear();
                        _builder.Append(previous);
                        Rewrite(length);
                        _cursor.MoveTo(Length);
                        continue;
                    }
                    case ConsoleKey.DownArrow:
                    {
                        if (!_redo.Any())
                        {
                            Console.Beep();
                            continue;
                        }

                        if (Length > 0)
                        {
                            _cursor.MoveTo(0);
                            _undo.Push(_builder.ToString());
                        }

                        var next = _redo.Pop();
                        var length = Length;
                        _builder.Clear();
                        _builder.Append(next);
                        Rewrite(length);
                        _cursor.MoveTo(Length);
                        continue;
                    }
                    case ConsoleKey.LeftArrow:
                        _cursor.MoveLeft();
                        continue;
                    case ConsoleKey.Home:
                        _cursor.MoveLeft(Length);
                        continue;
                    case ConsoleKey.RightArrow:
                        _cursor.MoveRight();
                        continue;
                    case ConsoleKey.End:
                        _cursor.MoveRight(Length);
                        continue;
                    case ConsoleKey.Backspace:
                        RemoveLeft();
                        continue;
                    case ConsoleKey.Delete:
                        RemoveRight();
                        continue;
                    case ConsoleKey.Enter:
                        goto end;
                    case ConsoleKey.Tab:
                        Insert("    ");
                        continue;
                    case ConsoleKey.V when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                        Insert(ClipboardService.GetText() ?? string.Empty);
                        continue;
                    default:
                        Insert(key.KeyChar.ToString());
                        continue;
                }
            }

            end:
            var str = _builder.ToString();
            Console.Write(new string('\b', str.Length + 1));
            Console.Write(new string(' ', str.Length + 1));
            Console.Write(new string('\b', str.Length + 1));
            return str;
        }

        /// <summary>
        /// Rewrites the entirety to the right side of the cursor.
        /// </summary>
        /// <param name="additionalCharacters">additional characters to clear</param>
        private void Rewrite(int additionalCharacters)
        {
            var index = _cursor.Index;
            _cursor.MoveTo(0);
            _cursor.MoveTo(_builder.Length);
            for (var i = 0; i <  + additionalCharacters; i++) 
                Console.Write(' ');
            for (var i = 0; i <  + additionalCharacters; i++) 
                Console.Write('\b');
            _cursor.MoveTo(index);
        }

        private void Insert(string s)
        {
            _builder.Insert(_cursor.Index, s);
            _cursor.MoveRight(s.Length);
            Rewrite(0);
        }
    }

    public static string ReadLine(Stack<string> undo, Stack<string> redo, CancellationToken cancellationToken)
    {
        return new InputLineHandler(undo, redo).ReadLine(cancellationToken);
    }
}