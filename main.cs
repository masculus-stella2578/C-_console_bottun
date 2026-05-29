using System;
using System.Data;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using NPOI.HPSF;
using NPOI.SS.Util;
using Array = System.Array;
using Org.BouncyCastle.Bcpg.Sig;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ICSharpCode.SharpZipLib.Zip;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using NPOI.SS.Formula.Functions;
using NPOI.OpenXmlFormats.Spreadsheet;
using System.Windows.Forms;

namespace src
{
    //---- 7/4メモ ----
    //それぞれのクラスの責任があやふやになってきてる。rockedのところとか特に。今一度どのクラスがどこまでの責任を担当して、それが最適なのかという確認、そして見直しを行う


    //9/27メモ
    //いまconsol.writeline(str)としてるところをList<string>.append(str);に置き換えて最後にfor文で回して全部書く。
    //9/30
    //じゃなくて、先頭行と最終行のlineを変数としてもっとけば、そこからその範囲内に該当するところを随時描画していけばいい。ただ、何行描画されるか先に知ることがでいないから、どこまでも下に行けてまうんちゃう？

    //
    //

    public class InputedOtherThanEnglishOrJapaneseException : Exception
    {
        public InputedOtherThanEnglishOrJapaneseException() { }

        public InputedOtherThanEnglishOrJapaneseException(string message)
            : base(message)
        {
        }

        public InputedOtherThanEnglishOrJapaneseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    public class ConsoleBuffer
    {
        private bool write_before = false;
        static Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
        public int total_lines { get; private set; } = 1;
        private int old_tota_lines = 1;
        private int buffer_len = 0;
        private int vartical_buffer_len = 0;
        private bool old_write_before;
        private bool clear_stop = false;
        private bool cursol_fixed = false;
        public List<int> chars_in_line { get; set; } = new();
        public List<int> old_chars_in_line { get; set; } = new();
        public List<bool> w_deleted = new();
        private int? top_to_fix = null;
        StringBuilder page_stringb = new();
        public ConsoleBuffer(int buffer_len = 0)
        {
            this.buffer_len = buffer_len;
        }


        public void FixCursolTop(int? top)
        {
            if (top != null && 0 <= top && top <= total_lines - 1)
            {
                if (top < Console.WindowHeight / 2)
                {
                    Console.SetCursorPosition(0, 0);
                }
                else
                {
                    Console.SetCursorPosition(0, top - Console.WindowHeight / 2 ?? 0);
                }
                Console.SetCursorPosition(0, top ?? 0);
                cursol_fixed = true;
            }
        }

        public void Clear()
        {
            clear_stop = false;
            buffer_len = Console.BufferWidth;
            //buffer_len = 100;
            Console.SetCursorPosition(0, 0);
            old_tota_lines = total_lines;
            old_write_before = write_before;
            for (int i = 0; i < chars_in_line.Count; i++)
            {
                old_chars_in_line[i] = chars_in_line[i];
                chars_in_line[i] = 0;

                w_deleted[i] = false;
            }
            total_lines = 0;
        }

        public void ClearStop()
        {
            OutOfPagePrevisousSpace();
            PageDeletePreviousSpace();
            clear_stop = true;
        }

        private void OutOfPagePrevisousSpace()
        {
            var sb = new StringBuilder();
            int line_times = 0;
            int index = 0;
            line_times = old_tota_lines - total_lines;
            index = total_lines;
            Console.SetCursorPosition(0, total_lines);
            if (chars_in_line.Count != 0)
            {
                for (int i = 0; i < line_times; i++)
                {
                    sb.Clear();
                    for (int ii = 0; ii < old_chars_in_line[index + i]; ii++)
                    {
                        sb.Append(" ");
                    }
                    Console.Write(sb.ToString());
                    Console.WriteLine("");
                }
                Console.SetCursorPosition(0, total_lines - 1);
            }
        }

        private void PageDeletePreviousSpace()
        {
            int times = 0;
            var sb = new StringBuilder();
            if (write_before) times = total_lines; else times = total_lines - 1;
            times = total_lines;
            for (int i = 0; i < times; i++)
            {
                if (chars_in_line[i] < buffer_len)
                {
                    Console.SetCursorPosition(chars_in_line[i], i);
                    sb.Clear();
                    for (int ii = 0; ii < old_chars_in_line[i] - chars_in_line[i]; ii++)
                    {
                        sb.Append(" ");
                    }
                    Console.Write(sb.ToString());
                }
            }
            double a = 1.1e1;
            Console.SetCursorPosition(chars_in_line[total_lines - 1], total_lines - 1);
        }

        public void CharsInLineAddSpace()
        {
            for (int i = 0; i < total_lines - chars_in_line.Count; i++)
            {
                chars_in_line.Add(0);
                old_chars_in_line.Add(0);
                w_deleted.Add(false);
            }
        }

        //sjis_がbyfferlneを超えないようにするのが目標。今のところの原因の有力候補は、total_lineと、charas_in_lineとのインデックスの関係がうまくいってなくて、そこでなぞに上乗せされて、オーバーしてるのではないか。そこから、>に反応してしまって、どんどん+1の奴が知らんうちにされてるできな。
        public void WriteLineProccess(string str, bool writeline)
        {
            //繰り返しの記述を避けるためのローカル関数を以下に定義.
            void WriteLinePakage()
            {
                Console.WriteLine("");
                total_lines++;
                CharsInLineAddSpace();
            }
            //end.

            int str_sjiscount = 0;
            int previous_i = 0;
            int sjiscont_now = 0;
            bool wrtei_before_skipper = false;
            CharsInLineAddSpace();
            if (total_lines == 0)
            {
                total_lines++;
                CharsInLineAddSpace();
            }
            if (cursol_fixed)
            {
                Console.SetCursorPosition(chars_in_line[total_lines - 1], total_lines - 1);
            }
            if (str == "")
            {
                if (writeline)
                {
                    WriteLinePakage();
                }
            }
            if (write_before)
            {
                CharsInLineAddSpace();
                str_sjiscount = chars_in_line[total_lines - 1];
                //Console.Write(chars_in_line[total_lines - 1]);
            }
            for (int i = 0; i < str.Length; i++)
            {
                str_sjiscount += sjisEnc.GetByteCount(str[i].ToString());
                sjiscont_now += sjisEnc.GetByteCount(str[i].ToString());
                if (str_sjiscount == buffer_len && i < str.Length - 1)
                {
                    Console.Write(str.Substring(previous_i, i - previous_i + 1));
                    previous_i = i + 1;
                    chars_in_line[total_lines - 1] += sjiscont_now;
                    WriteLinePakage();
                    //Console.WriteLine(sjiscont_now);
                    str_sjiscount = 0;
                    sjiscont_now = 0;
                }
                else if (str_sjiscount > buffer_len && i < str.Length - 1)
                {
                    Console.Write(str.Substring(previous_i, i - previous_i) + " ");
                    previous_i = i;
                    chars_in_line[total_lines - 1] += sjiscont_now;
                    WriteLinePakage();
                    //Console.WriteLine(sjiscont_now);
                    str_sjiscount = 2;
                    sjiscont_now = 2;
                    Console.WriteLine("e");
                }
                else if (str_sjiscount == buffer_len && i == str.Length - 1)
                {
                    Console.Write(str.Substring(previous_i, i - previous_i + 1));
                    chars_in_line[total_lines - 1] += sjiscont_now;
                    WriteLinePakage();
                    //Console.WriteLine(sjiscont_now);
                    str_sjiscount = 0;
                    sjiscont_now = 0;
                }
                else if (str_sjiscount > buffer_len && i == str.Length - 1)
                {
                    Console.Write(str.Substring(previous_i, i) + " ");
                    chars_in_line[total_lines - 1] += sjiscont_now;
                    WriteLinePakage();
                    str_sjiscount = 2;
                    sjiscont_now = 2;
                    Console.Write(str.Substring(str.Length - 1, 1));
                    CharsInLineAddSpace();
                    chars_in_line[total_lines - 1] += 2;
                    if (writeline)
                    {
                        WriteLinePakage();
                    }
                }
                else if (str_sjiscount < buffer_len && i == str.Length - 1)
                {
                    Console.Write(str.Substring(previous_i, str.Length - previous_i));
                    chars_in_line[total_lines - 1] += sjiscont_now;
                    if (writeline)
                    {
                        WriteLinePakage();
                    }
                    str_sjiscount = 0;
                    sjiscont_now = 0;
                }
            }
            if (writeline)
            {
                write_before = false;
            }
            else
            {
                write_before = true;
            }
        }


        //以下、WriteLineの引数型によるオーバーライド
        public void WriteLine<T>(T t)
        {
            WriteLineProccess(t.ToString() ?? "", true);
            /*
            if (t.)
            {

            }
            */
        }


        //以下、Writeの引数型によるオーバーライド
        public void Write<T>(T args)
        {
            WriteLineProccess(args.ToString() ?? "", false);
        }
    }

    internal class CSharpConsoleTUI
    {
        private RenderingForConsole renderingForConsole;
        private KeyEventHandler keyEventHandler;
        private IKeyEvent keyEvent;

        public CSharpConsoleTUI()
        {
            keyEvent = new KeyEvent();
            renderingForConsole = new(keyEvent);
            keyEventHandler = new(4, keyEvent);
        }
    }

    public interface IKeyEvent
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        const int VK_CONTROL = 0x11;
        const int VK_S = 0x53;
        const int VK_ESCAPE = 0x1B;
        const int VK_UP = 0x26;
        const int VK_DOWN = 0x28;
        const int VK_R = 0x27;
        const int VK_L = 0x25;
        const int VK_SPACE = 0x0D;
        const int VK_SHIFT = 0x10;
        public bool ctrlPressed { get; set; }
        bool sPressed { get; set; }
        public bool escPressed { get; set; }
        public bool up_pressd { get; set; }
        public bool down_pressd { get; set; }
        public bool right_pressd { get; set; }
        public bool left_pressd { get; set; }
        public bool space_pressd { get; set; }
        public bool shift_pressed { get; set; }

        public void ReDefine();
    }

    //本番環境
    public class KeyEvent : IKeyEvent
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        const int VK_CONTROL = 0x11;
        const int VK_S = 0x53;
        const int VK_ESCAPE = 0x1B;
        const int VK_UP = 0x26;
        const int VK_DOWN = 0x28;
        const int VK_R = 0x27;
        const int VK_L = 0x25;
        const int VK_SPACE = 0x0D;
        const int VK_SHIFT = 0x10;
        public bool ctrlPressed { get; set; } = ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0);
        public bool sPressed { get; set; } = ((GetAsyncKeyState(VK_S) & 0x8000) != 0);
        public bool escPressed { get; set; } = ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0);
        public bool up_pressd { get; set; } = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
        public bool down_pressd { get; set; } = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
        public bool right_pressd { get; set; } = ((GetAsyncKeyState(VK_R) & 0x8000) != 0);
        public bool left_pressd { get; set; } = ((GetAsyncKeyState(VK_L) & 0x8000) != 0);
        public bool space_pressd { get; set; } = ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0);
        public bool shift_pressed { get; set; } = ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0);
        public void ReDefine()
        {
            up_pressd = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
            down_pressd = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
            right_pressd = ((GetAsyncKeyState(VK_R) & 0x8000) != 0);
            left_pressd = ((GetAsyncKeyState(VK_L) & 0x8000) != 0);
            space_pressd = ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0);
            shift_pressed = ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0);
        }
    }

    internal class KeyEventHandlerElement
    {
        protected const int number_of_available_key = 16;
    }

    internal enum KeyToIndex
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Shift = 4,
        Enter = 5,
        W = 6,
        S = 7,
        A = 8,
        D = 9,
        Shift_W = 10,
        Shift_S = 11,
        Shift_A = 12,
        Shift_D = 13,
        Shift_Q = 14,
        Shift_E = 15
    }

    internal class KeyEventHandlerOneThread : KeyEventHandlerElement
    {

        private List<Action>[] actions = new List<Action>[number_of_available_key];
        private IKeyEvent keyEvent;
        private Thread thread;
        public KeyEventHandlerOneThread(IKeyEvent keyEvent)
        {
            this.keyEvent = keyEvent;
            thread = new Thread(this.KeyEventHandl);
        }

        public void SetOneKeyAction(int index, List<Action> arg_actions)
        {
            actions[index] = arg_actions;
        }

        public void ExecuteActions(KeyToIndex keyToIndex)
        {
            foreach (Action action in actions[(int)keyToIndex])
            {
                action();
            }
        }

        public void KeyEventHandl()
        {
            while (true)
            {
                if (keyEvent.up_pressd)
                {
                    ExecuteActions(KeyToIndex.Up);
                }
                if (keyEvent.down_pressd)
                {
                    ExecuteActions(KeyToIndex.Down);
                }
                if (keyEvent.right_pressd)
                {
                    ExecuteActions(KeyToIndex.Right);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Left);
                }

                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.W);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.S);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.D);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.A);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Shift_W);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Shift_S);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Shift_D);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Shift_A);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Shift_Q);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Shift_E);
                }
                if (keyEvent.left_pressd)
                {
                    ExecuteActions(KeyToIndex.Enter);
                }
            }
        }

        public void Clear()
        {
            Array.Clear(actions, 0, actions.Length);
        }

        public void Start()
        {
            thread.Start();
        }

        public void Stop()
        {
            thread.Join();
        }
    }

    internal class KeyEventHandler : KeyEventHandlerElement
    {
        private List<KeyEventHandlerOneThread> thread_list = new();
        private int[] is_already_added_action = new int[number_of_available_key];
        private int mux_index_of_thread = 0;
        private int max_thread_num = 0;
        private IKeyEvent keyEvent;
        public KeyEventHandler(int max_thread_num, IKeyEvent keyEvent)
        {
            this.max_thread_num = max_thread_num;
            this.keyEvent = keyEvent;
        }

        public void AddThread()
        {
            var t = new KeyEventHandlerOneThread(keyEvent);
            thread_list.Add(t);
        }

        public void AddAction(int thread_index, KeyToIndex key_to_index, params Action[] actions)
        {
            var list = new List<Action>(actions);
            thread_list[thread_index].SetOneKeyAction((int)key_to_index, list);
        }

        public void ActionClear(int? thread_index = null)
        {
            if (thread_index == null)
            {
                foreach (var item in thread_list)
                {
                    item.Clear();
                }
                return;
            }
            thread_list[(int)thread_index].Clear();
        }

        public void StartAll()
        {
            foreach (var key_thread in thread_list)
            {
                key_thread.Start();
            }
        }

        public void StopAll()
        {
            foreach (var key_thread in thread_list)
            {
                key_thread.Stop();
            }
        }

        public void Start(params int[] thread_indexs)
        {
            foreach (int i in thread_indexs)
            {
                thread_list[i].Start();
            }
        }

        public void Stop(params int[] thread_indexs)
        {
            foreach (int i in thread_indexs)
            {
                thread_list[i].Stop();
            }
        }
    }

    internal interface IRenderingElementRelatedInConsoleSize
    {
        public void ReSetConsoleSize();
    }

    //manage sheet queue and enteier using rendering for console
    internal class RenderingForConsole : IRenderingElementRelatedInConsoleSize
    {
        private List<Sheet> sheet_q;
        private Sheet? sheet_to_render;
        private IKeyEvent keyEvent;
        private List<StringBuilder> text_to_write = new();
        private StringBuilder fainal_sb_to_write = new();
        private int console_y_length = 0;
        private int sheet_serial_num = 0;

        public RenderingForConsole(IKeyEvent keyEvent)
        {
            sheet_q = new List<Sheet>();
            sheet_to_render = null;
            this.keyEvent = keyEvent;
        }

        public void ReSetConsoleSize()
        {
            int to = Console.WindowHeight;
            while ( console_y_length > to)
            {
                text_to_write.Add(new StringBuilder());
            }
        }

        public void AddSheet(Sheet? arg_sheet = null, int index = -1)
        {
            if (arg_sheet == null)
            {
                arg_sheet = new Sheet(keyEvent);
            }
            arg_sheet.serial_number = sheet_serial_num++;

            if (index < 0)
            {
                sheet_q.Add(arg_sheet);
            }
            else
            {
                sheet_q.Insert(index, arg_sheet);
            }
        }

        public Sheet GetSheet(int inndex)
        {
            return sheet_q[inndex];
        }

        public void SetSheetToRender(int index)
        {
            sheet_to_render = sheet_q[index];
        }

        public void RenderingOnConsole()
        {
            if (sheet_to_render == null)
            {
                return;
            }

            for (int text_to_write_y = 0; text_to_write_y < console_y_length; text_to_write_y++)
            {
                //変更されているかされていないか判定.
                //されていたら次のコンソール行へ.
                bool no_section_is_changed = true;
                foreach (SectionInfoInLine sectionInfoInLine in sheet_to_render.applicable_sections_in_line[text_to_write_y])
                {
                    Section section = sheet_to_render.GetSection(sectionInfoInLine.section_serial_num);
                    int section_y = section.Page_starting_y_pos + sectionInfoInLine.line_serial;
                    bool no_layer_is_changed = true;
                    foreach (SectionLayer layer in section.layers)
                    {
                        if (layer.texts[section_y].Is_changed)
                        {
                            no_layer_is_changed = false;
                            break;
                        }
                    }
                    if (!no_section_is_changed)
                    {
                        no_section_is_changed = false;
                        break;
                    }
                }
                if (no_section_is_changed)
                {
                    continue;
                }

                //以下変更有.
                text_to_write[text_to_write_y].Clear();
                foreach (SectionInfoInLine sectionInfoInLine in sheet_to_render.applicable_sections_in_line[text_to_write_y])
                {
                    Section section = sheet_to_render.GetSection(sectionInfoInLine.section_serial_num);
                    int section_y = section.Page_starting_y_pos + sectionInfoInLine.line_serial;

                    //このyについて
                    //英字分一文字ずつ書いていく
                    int section_x = section.Page_starting_x_pos;
                    int section_x_from_zero = 0;
                    int text_to_write_x = 0;
                    while (section_x < section.Page_starting_x_pos + section.X_span)
                    {
                        if (section_x == section.Length_in_English - 1)
                            break;

                        SectionLayer layer;
                        CharType charType;
                        for (int i = 0, layer_index = section.layers.Count - 1; i < section.layers.Count; i++, layer_index--)
                        {
                            layer = section.GetSectionLayer(layer_index);
                            charType = layer.texts_info[section_y][section_x].type;

                            switch (charType)
                            {
                                case CharType.Empty:
                                    if (layer_index == 0)
                                    {
                                        text_to_write[text_to_write_y][text_to_write_x] = ' ';
                                    }
                                    continue;

                                case CharType.English:
                                    text_to_write[text_to_write_y][text_to_write_x] = layer.texts[section_y].sb[layer.texts_info[section_y][section_x].text_index];
                                    section_x++;
                                    goto break_for;

                                case CharType.JapaneseStart:
                                    if (section_x + 2 > section.Page_starting_x_pos + section.X_span)
                                    {
                                        text_to_write[text_to_write_y][text_to_write_x] = ' ';
                                    }
                                    else
                                    {
                                        text_to_write[text_to_write_y][text_to_write_x] = layer.texts[section_y].sb[layer.texts_info[section_y][section_x].text_index];
                                        section_x += 2;
                                    }
                                    goto break_for;

                                default:
                                    break;

                            }
                        }

                    break_for:
                        ;
                    }

                    //余りの空白部分を埋める.
                    for (int i = 0; i < section.X_span - section_x_from_zero - 1; i++, text_to_write_x++)
                    {
                        text_to_write[text_to_write_y][text_to_write_x] = ' ';
                    }

                }
            }

            for (int i = 0; i < console_y_length; i++)
            {
                fainal_sb_to_write.Append(text_to_write[i]);
                if (i != console_y_length - 1)
                {
                    fainal_sb_to_write.Append("\n");
                }
            }
            Console.SetCursorPosition(0, 0);
            Console.Write(fainal_sb_to_write);
        }

    }

    internal class SectionInfoInLine
    {
        public SectionInfoInLine(int section_serial_num, int line_serial)
        {
            this.section_serial_num = section_serial_num;
            this.line_serial = line_serial;
        }
        public int section_serial_num;
        public int line_serial;
    }

    internal class Sheet
    {
        private class SectionSerialNumberAndXpos
        {
            public SectionSerialNumberAndXpos(int serial_num, int x_pos)
            {
                this.serial_num = serial_num;
                this.xpos = x_pos;
            }
            public int serial_num;
            public int xpos;
        }

        public int serial_number { get; set; }
        private int section_serial_number = 0;
        private List<Section> sections;
        public List<List<SectionInfoInLine>> applicable_sections_in_line { get; private set; }
        private List<int> section_serial_number_s = new();
        private List<int> section_x_pos_list = new();
        private List<SectionSerialNumberAndXpos> serial_and_xpos = new();
        private IKeyEvent keyEvent;

        public Sheet(IKeyEvent keyEvent)
        {
            sections = new List<Section>();
            this.keyEvent = keyEvent;
        }

        void XposSort()
        {
            int n = serial_and_xpos.Count;

            for (int i = 1; i < n; i++)
            {
                var temp = serial_and_xpos[i];
                int j = i - 1;

                while (j >= 0 &&
                       serial_and_xpos[j].xpos > temp.xpos)
                {
                    serial_and_xpos[j + 1] = serial_and_xpos[j];
                    j--;
                }

                serial_and_xpos[j + 1] = temp;
            }
        }

        public List<List<SectionInfoInLine>> GetSectionsInLine()
        {
            return applicable_sections_in_line;
        }

        public void AddSection(int x_pos, int x_span, int y_pos, int y_span, int index = -1, Section? arg_section = null)
        {
            if (arg_section == null)
            {
                arg_section = new Section(keyEvent);
            }
            arg_section.serial_number = section_serial_number++;
            arg_section.X_pos = x_pos;
            arg_section.Y_pos = y_pos;
            arg_section.Y_span = y_span;
            arg_section.X_span = x_span;

            if (index < 0)
            {
                sections.Add(arg_section);
            }
            else
            {
                sections.Insert(index, arg_section);
            }

            serial_and_xpos.Add(new SectionSerialNumberAndXpos(sections[sections.Count - 1].serial_number, sections[sections.Count - 1].X_pos));
            ResetSectionsInfoInLine();
        }

        private void ResetSectionsInfoInLine()
        {
            XposSort();

            foreach (var list in applicable_sections_in_line)
            {
                list.Clear();
            }

            foreach (var each in serial_and_xpos)
            {
                int serial_num = each.serial_num;
                for (int i = sections[serial_num].Y_pos, line_serial = 0; i < applicable_sections_in_line.Count; i++)
                {
                    applicable_sections_in_line[i].Add(new SectionInfoInLine(serial_num, line_serial)); 
                    line_serial++;
                }
            }
        }

        public Section GetSection(int inndex)
        {
            return sections[inndex];
        }

        public List<Section> GetSections()
        {
            return sections;
        }
    }

    internal class Section : IRenderingElementRelatedInConsoleSize
    {
        public int serial_number { get; set; }
        public int X_pos { get; set; }
        public int X_span { get; set; }
        public int Y_pos { get; set; }
        public int Y_span { get; set; }

        public bool Is_changed_in_page { get; set; }

        public int Page_starting_y_pos { get; set; }
        public int Page_starting_x_pos { get; set; }

        public int Total_writed_line_count { get; set; } = 0;
        public int Length_in_English { get; set; } = new();
        private int section_layer_serial_num = 0;

        public List<SectionLayer> layers { get; set; }
        private IKeyEvent keyEvent;

        public Section(IKeyEvent keyEvent)
        {
            layers = new List<SectionLayer>();
            this.keyEvent = keyEvent;
        }

        public void ReSetConsoleSize()
        {

        }

        public int GetPageFinishingY()
        {
            return Page_starting_y_pos + Y_pos - 1;
        }

        public int GetPageFinishingX()
        {
            return Page_starting_x_pos + X_pos - 1;
        }

        public void AddSectionLayer(SectionLayer? arg_layer = null, int index = -1)
        {
            if (arg_layer == null)
            {
                arg_layer = new SectionLayer(keyEvent, this);
            }
            arg_layer.serial_number = section_layer_serial_num++;

            if (index < 0)
            {
                layers.Add(arg_layer);
            }
            else
            {
                layers.Insert(index, arg_layer);
            }
        }

        public SectionLayer GetSectionLayer(int inndex)
        {
            return layers[inndex];
        }

        private void Wait()
        {
            while (keyEvent.up_pressd | keyEvent.down_pressd | keyEvent.right_pressd | keyEvent.left_pressd | keyEvent.space_pressd)
            {
                Thread.Sleep(1);
            }
        }
        public void UpPage(bool for_key)
        {
            int previous_page_y = Page_starting_y_pos;
            if (Page_starting_y_pos != 0) {
                Page_starting_y_pos -= Y_span;
                if (Page_starting_y_pos < 0)
                {
                    Page_starting_y_pos = 0;
                }
            }

            if (previous_page_y != Page_starting_y_pos)
            {
                Is_changed_in_page = true;
            }
            if (for_key)
            {
                Wait();
            }
        }
        public void DownPage(bool for_key)
        {
            int previous_page_y = Page_starting_y_pos;
            Page_starting_y_pos += Y_span;
            if (Page_starting_y_pos + Y_span > Total_writed_line_count)
            {
                Page_starting_y_pos = Total_writed_line_count - Y_span;
            }

            if (previous_page_y != Page_starting_y_pos)
            {
                Is_changed_in_page = true;
            }
            if (for_key)
            {
                Wait();
            }
        }
        public void LeftSlidePage(bool for_key)
        {
            int previous_x_pos = Page_starting_x_pos;
            if (Page_starting_x_pos != 0)
            {
                Page_starting_x_pos -= X_span;
                if (Page_starting_x_pos < 0)
                {
                    Page_starting_x_pos = 0;
                }
            }

            if (previous_x_pos != Page_starting_x_pos)
            {
                Is_changed_in_page = true;
            }
            if (for_key)
            {
                Wait();
            }
        }
        public void RightSlidePage(bool for_key)
        {
            int previous_x_pos = Page_starting_x_pos;
            Page_starting_x_pos += X_span;
            if (Page_starting_x_pos > Length_in_English)
            {
                Page_starting_x_pos = Length_in_English - X_span;
            }

            if (previous_x_pos != Page_starting_x_pos)
            {
                Is_changed_in_page = true;
            }
            if (for_key)
            {
                Wait();
            }
        }
    }
    internal enum CharType
    {
        Empty = 0,
        English = 1,
        JapaneseStart = 2,
        JapaneseEnd = 3,
    }

    internal class SectionLayerTextInfo
    {
        public SectionLayerTextInfo(int text_i)
        {
            text_index = text_i;
        }
        public int text_index = -1;
        public CharType type = CharType.Empty;
    }

    internal class SectionLayerTextsAndLenght
    {
        public StringBuilder sb = new StringBuilder();
        public int length_in_English = 0;
        public bool Is_changed = false;
    }

    internal class SectionLayer : IRenderingElementRelatedInConsoleSize
    {
        public int serial_number { get; set; }
        public List<SectionLayerTextsAndLenght> texts { get; private set; }
        public List<List<SectionLayerTextInfo>> texts_info { get; private set; }
        public int Total_writed_line_count { get; set; } = 0;
        private int previous_current_y_when_write = 0;
        private int current_x = 0;
        private int current_y = 0;
        private Section parent_section;
        private bool is_fix_current_x = false;

        public bool IsClearedCurrently { get; set; } = true;

        public SectionLayer(IKeyEvent keyEvent, Section parent_section)
        {
            texts = new List<SectionLayerTextsAndLenght>();
            texts_info = new List<List<SectionLayerTextInfo>>();
            this.parent_section = parent_section;
        }

        public void ReSetConsoleSize()
        {

        }

        public int GetLenOfTextsList()
        {
            return texts.Count;
        }

        public void Clear()
        {
            current_x = 0;
            current_y = 0;
            previous_current_y_when_write = 0;
            Total_writed_line_count = 0;
            foreach (var text in texts)
            {
                text.sb.Clear();
            }
            foreach (var info_in_line in texts_info)
            {
                foreach(var info in info_in_line)
                {
                    info.type = CharType.Empty;
                    info.text_index = 0;
                }
            }
        }

        public void SetCursolPos(int x, int y)
        {
            current_y = y;
            current_x = x;
        }

        public void FixX()
        {
            is_fix_current_x = true;
        }

        private bool IsInRangeOfPage()
        {
            return true;
        }

        private enum SimpleCharType
        {
            None,
            English,
            Japanese
        }

        private SimpleCharType JudgeCharType(char c)
        {
            if ((uint)(c - 'A') <= 'Z' - 'A' ||
                    (uint)(c - 'a') <= 'z' - 'a') //英語
            {
                return SimpleCharType.English;
            }
            else if (
                (uint)(c - '\u3040') <= '\u309F' - '\u3040' ||
                (uint)(c - '\u30A0') <= '\u30FF' - '\u30A0' ||
                (uint)(c - '\u4E00') <= '\u9FFF' - '\u4E00') //日本語
            {
                return SimpleCharType.Japanese;
            }

            return SimpleCharType.None;
        }

        private void MakeUpYListsBlanckUntil(int y)
        {
            while (y > texts.Count - 1)
            {
                texts.Add(new SectionLayerTextsAndLenght());
            }
            while (y > texts_info.Count - 1)
            {
                texts_info.Add(new List<SectionLayerTextInfo>());
            }
        }

        private void MakeUpXListsBlanckUntil(int x)
        {
            while (x > texts[current_y].sb.Length - 1)
            {
                texts[current_y].sb.Append(" ");
            }
            while (x > texts_info[current_y].Count - 1)
            {
                texts_info[current_y].Add(new SectionLayerTextInfo(texts_info[current_y][x - 1].text_index + 1));
            }
        }

        private bool IsChangedInPage(int length)
        {
            //if parent_section have ever been changed &&
            //if current_y is in range of page
            return
            (
                parent_section.Is_changed_in_page == false &&
                length != 0 &&
                (current_y >= parent_section.Page_starting_y_pos && current_y <= parent_section.GetPageFinishingY() &&
                current_x >= parent_section.Page_starting_x_pos && current_x <= parent_section.GetPageFinishingX())
            );
        }
        private void SetInfoInEmptyAndProceedX(ref int text_i, ref int x)
        {
            texts_info[current_y][x].type = CharType.Empty;
            texts_info[current_y][x].text_index = text_i;

            x++;
        }
        private void SetInfoInEnglishAndProceedX(ref int text_i, ref int x)
        {
            texts_info[current_y][x].type = CharType.English;
            texts_info[current_y][x].text_index = text_i;

            x++;
        }
        private void SetInfoInJapaneseAndProceedX(ref int text_i, ref int x)
        {
            texts_info[current_y][x].type = CharType.JapaneseStart;
            texts_info[current_y][x + 1].type = CharType.JapaneseEnd;

            texts_info[current_y][x].text_index = text_i;
            texts_info[current_y][x + 1].text_index = text_i;

            x += 2;
        }

        private void SetTotalLineCountAndLength()
        {
            if (current_y > Total_writed_line_count - 1)
            {
                Total_writed_line_count = current_y + 1;
            }
            if (Total_writed_line_count > parent_section.Total_writed_line_count)
            {
                parent_section.Total_writed_line_count = Total_writed_line_count;
            }

            if (current_y > texts[current_y].length_in_English - 1)
            {
                texts[current_y].length_in_English = current_x;
            }
            if (texts[current_y].length_in_English > parent_section.Length_in_English)
            {
                parent_section.Length_in_English = texts[current_y].length_in_English;
            }
        }

        public SectionLayer WriteEmpty(int length)
        {
            MakeUpYListsBlanckUntil(current_y);

            //currentまで一旦.
            MakeUpXListsBlanckUntil(current_x);

            //差分で完全一致な帰る.
            if (texts_info[current_y][current_y].type != CharType.JapaneseEnd)
            {
                bool is_same = true;
                for (int i = 0, x = current_x; i < length; i++, x++)
                {
                    MakeUpXListsBlanckUntil(x);
                    if (texts_info[current_y][x].type != CharType.Empty)
                    {
                        is_same = false;
                        break;
                    }
                }
                if (is_same)
                {
                    current_x += length;
                    return this;
                }
            }

            if (length != 0)
            {
                texts[current_y].Is_changed = true;
            }
            parent_section.Is_changed_in_page = IsChangedInPage(length);

            int first_text_i = texts_info[current_y][current_x].text_index;
            int text_i = first_text_i;
            int first_add_blanck = 0;
            int end_add_blanck = 0;
            if (texts_info[current_y][current_x].type == CharType.JapaneseEnd)
            {
                current_x--;
                SetInfoInEmptyAndProceedX(ref text_i, ref current_x);
                first_add_blanck++;
            }

            for (int i = 0; i < length; i++)
            {
                SetInfoInEmptyAndProceedX(ref text_i, ref current_x);
                if (i == length - 1 && texts_info[current_y][current_x].type == CharType.JapaneseStart)
                {
                    end_add_blanck++;
                }
            }

            int end_text_i = texts_info[current_y][current_x].text_index;
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', length + first_add_blanck + end_add_blanck);
            texts[current_y].sb.Remove(first_text_i, end_text_i - first_text_i + 1);
            texts[current_y].sb.Insert(first_text_i, sb);

            SetTotalLineCountAndLength();

            return this;
        }

        public SectionLayer WriteLineEmpty(int length)
        {
            WriteEmpty(length);
            current_y++;
            return this;
        }

        public SectionLayer Write(string str)
        {
            MakeUpYListsBlanckUntil(current_y);
            MakeUpXListsBlanckUntil(current_x);

            //もし以前と変更なしなら帰る.
            if (texts_info[current_y][current_y].type != CharType.JapaneseEnd)
            {
                bool is_same = true;
                int current_x_proceed = current_x;
                for (int i = 0, index = texts_info[current_y][current_x].text_index; i < str.Length; i++, index++)
                {
                    MakeUpXListsBlanckUntil(current_x_proceed);

                    if (str[i] != texts[current_y].sb[index] && texts_info[current_y][current_x_proceed].type != CharType.Empty)
                    {
                        is_same = false;
                        break;
                    }
                    switch (JudgeCharType(str[i]))
                    {
                        case SimpleCharType.English:
                        case SimpleCharType.None:
                            current_x_proceed++;
                            break;
                        case SimpleCharType.Japanese:
                            current_x_proceed += 2;
                            break;
                    }
                }
                if (is_same)
                    current_x = current_x_proceed;
                    return this;
            }

            //以下以前と変更あり.
            if (str.Length != 0)
            {
                texts[current_y].Is_changed = true;
            }
            parent_section.Is_changed_in_page = IsChangedInPage(str.Length);


            int first_text_i = texts_info[current_y][current_x].text_index;
            int text_i = first_text_i;
            if (texts_info[current_y][current_x].type == CharType.JapaneseEnd)
            {
                str = " " + str;
                current_x--;
                SetInfoInEnglishAndProceedX(ref text_i, ref current_x);
            }
            for (int str_i = 0; str_i < str.Length; str_i++)
            {
                MakeUpXListsBlanckUntil(current_x);

                char c = str[str_i];
                switch (JudgeCharType(c))
                {
                    case SimpleCharType.English:
                        SetInfoInEnglishAndProceedX(ref text_i, ref current_x);
                        break;

                    case SimpleCharType.Japanese:
                        SetInfoInJapaneseAndProceedX(ref text_i, ref current_x);
                        break;

                    case SimpleCharType.None:
                        SetInfoInEnglishAndProceedX(ref text_i, ref current_x);
                        throw new InputedOtherThanEnglishOrJapaneseException("SectionLayerのWrite/WriteLine関数に関して、英語と日本語以外が入力されました。");
                }

                if (str_i == str.Length - 1 && texts_info[current_y][current_x].type == CharType.JapaneseStart)
                {
                    str = str + " ";
                    SetInfoInEnglishAndProceedX(ref text_i, ref current_x);
                }
            }

            int end_text_i = texts_info[current_y][current_x].text_index;
            texts[current_y].sb.Remove(first_text_i, end_text_i - first_text_i + 1);
            texts[current_y].sb.Insert(first_text_i, str);

            SetTotalLineCountAndLength();

            return this;
        }

        public void WriteLine(string str)
        {
            Write(str);
            current_y++;
        }
    }

    //if (previous_length > now_rength)
    //  if (layer(now_layer_num - 1))
    //      前のレイヤーにあるテキストで短くなった分をうめる
    //  else
    //      空白で同じく埋める


    //変更有セクション中の変更有レイヤーのテキストのみを描画する。複数ある場合は回す.

    //ではページはどうする？
    //class Bottun ->
    //in SetSelected()
    //if (Bottun.line is out of page limit line)
    //  make page_y (Bottun.line / page_lien_span (then if % != 0 this += 1))
    //if (Bottun.pos is out of page pos line)
    //  make page_x (Bottun.pos / page_pos_span (then if % != 0 this += 1))

    //if 

    //ボタン内にセクションでの行番号と開始位置、スパンを保持させておき、
    //それを引数として、layer.write(bottun.section_line_num, bottun.section_pos_num + span, ...)みたいにする。
    //layer(1).fix_pos(bottun.section_pos_num)
    //layer(1).set_line(bottun.section_line_num)
    //
    //以下をまとめて、write_after_select_element_bottun()
    //layer(1).write()...
    //layer(1).write()...
    //
    //cnosole.Write();
    //
    //BottunsAfterSelectElement.RockOn();
    //

    //start
    //
    //console.StartKeyHanle();
    //console.UseThreadTo(console);
    //
    //in console
    //if (up_pressed)
    //x++;
    //if (enter_pressed)
    //console.UseThreadTo(current_sheet);
    //
    //in sheet
    //if (enter_pressed)
    //console.UseThreadTo(current_section);
    //
    //

    //public main_memu

    //public void ReWriteToMemuSection()
    //  Section to_menu_section = tounament_sheet.Section(0);
    //  to_memu_section.Layer(0).Write(to_memu_bottun, line_num : 0, how_many_space_between_buttons : 4);

    //public void ReWriteInputSection()

    //public ReWriteAll()
    //  ReWriteToMemuSection();
    //  ReWriteToInputSection();

    //
    //KeyEvent key_evnet = new KeyEvent(max_thread : 3);

    //key_event.ClearAction();
    //key_event.SetAction("W", to_menu.Up(key_event), ReWrite())
    //key_event.SetAction("S", ...)
    //key_event.SetAction("Shift", "W", to_menu.Up())

    //Sheet tounament_sheet = new();
    //tounament_sheet.AddSection(times : 3)
    //to_memu_buttuns.AddButtun("戻る", Action action = Finish()).AddButtun("説明", )
    //to_memu_buttuns.WriteWhenSelected(ReWriteInputSection)
    //tounament_buttuns.AddButtun("＋", Action action = AddFunction()), Action action = WriteMainInput())
    //for (int i ...)
    //  add_person_buttns.AddBtn(dic[i], Action action = AddProcess())
    //ReWriteAll();

    //public WriteMainInput()

    //public void AddFunction()
    //  WriteAddFunction();
    //  add_person_btns.RockOn();

    //public void WriteAddFunction()
    //  input_section.Layer(1).SetStartPointOfCursol(input_buttun_write.Selected());
    //  input_section.Layer(1).RockStartXOfcursol(input_button_weite.Selecter());
    //
    //  for (int i...)
    //      layer.Write(add_person_buttun);

    //public void WriteToThisPerson()
    //  input_section.Layer(1).SetStartPointOfCursol(input_buttun_write.Selected());
    //  input_section.Layer(1).RockStartXOfcursol(input_button_weite.Selecter());
    //
    //  Layer layer = input_section.Layer(1);
    //  layer.Clear();
    //  layer.WeiteLine("-", 8)
    //  layer.WriteLine("| ", tothis_person_buttns.btn(0, 0), " |");
    //  layer.WriteLine("| ", tothis_person_buttns.btn(0, 1), " |");
    //  layer.WeiteLine("-", 8)
    //

    //public void AddProcess()
    //  added_person_list[dic(rabel)] = true
    //  person_btns.AddBtns(rabel, PersonDeletMenu(), WriteMainFunction);
    //  person_btns.AddBtns("+", AddEventInPerson());

    //public void AddEventInPerson()
    //  WriteAddEventInPerson();
    //  event_btns.RockOn();

    //public AddEventProcess()
    //  person_btns[index].AddBtns(rabel, EventDeletOrChange(), )

    //public void ToThisPerson()
    //  WriteToThisPerson();
    //  tothis_person_btn.RockOn();

    public class PageQueue
    {

    }

    public class Page
    {

    }

    public class BottunSheetIndexer
    {

        public BottunSheetIndexer()
        {

        }


    }

    //
    public class BottunSheet
    {
        public BottunQ BQ { get; set; }
        private Thread t1;
        private Action WritePage;
        public BottunSheet(Action writePage)
        {
            WritePage = writePage;
            IKeyEvent keyEventHandler = new KeyEvent();
            BQ = new BottunQ(WritePage, keyEventHandler);
            t1 = new Thread(BQ.BottunSelect);
        }

        public void StartHandlingKeyEvent()
        {
            t1.Start();
        }
        public void StopHandlingKeyEvent()
        {
            t1.Join();
        }

        public void MainInKeyEvent()
        {

        }

    }

    //ボタンを一つのシート上でキューとして管理。ここでは、ボタンの選択にかかわる処理や、キューに保持しているボタンの要素を変えたりすることができる。
    //キーボード操作はもう一つ独立してクラス作ったら？

    public class BottunQ
    {
        private IKeyEvent keyEvent;
        private List<List<Bottun>> bottun_queue = new();
        private int bottun_x = 0;
        private int bottun_y = 0;
        private int old_x = 0;
        private int old_y = 0;
        private Action WritePage;
        private bool rocked { get; set; } = false;
        private bool dont_release { get; set; } = false;

        private bool some_on_rocked = false;

        private int y_length { get; set; } = 0;
        private int x_length { get; set; } = 0;

        public BottunQ(Action writePage, IKeyEvent keyEvent)
        {
            WritePage = writePage;
            this.keyEvent = keyEvent;
        }

        public void BeLocked()
        {
            rocked = true;
        }

        public void BeUnlocked()
        {
            rocked = false;
        }

        public void AddNewBottun(int y = -1, int x = -1, bool apeal = true, string rabel = "bottun", Action? function = null)
        {
            if (y < 0) y = bottun_queue.Count;
            if (x < 0) x = bottun_queue[y].Count;
            if (((0 <= y) && (y <= bottun_queue.Count)) && ((0 <= x) && (x <= bottun_queue[y].Count)))
            {
                bottun_queue[y].Insert(x, new Bottun(y, x, rabel, apeal));
                bottun_queue[y][x].Function = function;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void DereteBottun(int y = -1, int x = -1)
        {
            if (y < 0) y = bottun_queue.Count - 1;
            if (x < 0) x = bottun_queue[y].Count - 1;
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                bottun_queue[y].RemoveAt(x);
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void AddNewList(int size_of_list, int y = -1, bool apeal = true, string rabel = "bottun", Action? function = null)
        {
            if (y < 0)
            {
                y = bottun_queue.Count;
            }
            if (0 < size_of_list && 0 <= y && y <= bottun_queue.Count)
            {
                bottun_queue.Insert(y, new List<Bottun>());
                for (int i = 0; i < size_of_list; i++)
                {
                    bottun_queue[y].Add(new Bottun(bottun_queue.Count - 1, i, rabel, apeal));
                    bottun_queue[y][i].Function = function;
                }
            }
        }


        public void DereteList(int y = -1)
        {
            if (y < 0)
            {
                y = bottun_queue.Count - 1;
            }
            if (((0 <= y) && (y <= bottun_queue.Count - 1)))
            {
                bottun_queue.RemoveAt(y);
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void SetCursolTop(int y, int x)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                bottun_queue[y][x].console_top = Console.GetCursorPosition().Top;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void SetFunction(int y, int x, Action function)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                bottun_queue[y][x].Function = function;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void SetRabel(int y, int x, string rabel)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                bottun_queue[y][x].rabel = rabel;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void SetApear(int y, int x, bool apear)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                bottun_queue[y][x].apear = apear;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public bool GetOn(int y, int x)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                return bottun_queue[y][x].on;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
                return false;
            }
        }

        public bool GetSelected(int y, int x)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                return bottun_queue[y][x].selsected;
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
                return false;
            }
        }

        public string GetRabel(int y, int x)
        {
            if (((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)))
            {
                return bottun_queue[y][x].Rabel();
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
                return "";
            }
        }

        public int X()
        {
            return bottun_x;
        }

        public int Y()
        {
            return bottun_y;
        }
        public int Y_length()
        {
            return bottun_queue.Count;
        }

        public int X_length(int y)
        {
            return bottun_queue[y].Count;
        }

        public void Wait()
        {
            while (keyEvent.up_pressd | keyEvent.down_pressd | keyEvent.right_pressd | keyEvent.left_pressd | keyEvent.space_pressd)
            {
                Thread.Sleep(1);
            }
        }

        public void UpProcess()
        {
            if (rocked)
                return;

            if (!((0 < bottun_y) && !some_on_rocked))
                return;

            XYGetOld();
            if (bottun_x > bottun_queue[bottun_y - 1].Count - 1)
            {
                bottun_x = bottun_queue[bottun_y - 1].Count - 1;
            }
            bottun_y--;
            ChangeSelected();
            WritePage();
        }

        public void DownProcess()
        {
            if (rocked)
                return;

            if (!((bottun_y < bottun_queue.Count - 1) && !some_on_rocked))
                return;

            XYGetOld();
            if (bottun_x > bottun_queue[bottun_y + 1].Count - 1)
            {
                bottun_x = bottun_queue[bottun_y + 1].Count - 1;
            }
            bottun_y++;
            ChangeSelected();
            WritePage();
        }

        public void LeftSlideProcess()
        {
            if (rocked)
                return;

            if (!((0 < bottun_x) && !some_on_rocked))
                return;

            XYGetOld();
            bottun_x--;
            ChangeSelected();
            WritePage();
        }

        public void RightSlideProcess()
        {
            if (rocked)
                return;

            if (!((bottun_x < bottun_queue[bottun_y].Count - 1) && !some_on_rocked))
                return;

            XYGetOld();
            bottun_x++;
            ChangeSelected();
            WritePage();
        }

        public void UpSelected()
        {
            UpProcess();
            Wait();
        }

        public void DownSelected()
        {
            DownProcess();
            Wait();
        }

        public void LeftSlideSelected()
        {
            LeftSlideProcess();
            Wait();
        }

        public void RightSlideSelected()
        {
            RightSlideProcess();
            Wait();
        }

        public void FastUpSelected()
        {
            UpProcess();
            Thread.Sleep(500);
        }

        public void FastDownSelected()
        {
            DownProcess();
            Thread.Sleep(500);
        }

        public void FastLeftSlideSelected()
        {
            LeftSlideProcess();
            Thread.Sleep(500);
        }

        public void FastRightSlideSelected()
        {
            RightSlideProcess();
            Thread.Sleep(500);
        }

        public void TurnOnToKeyEvent()
        {
            if (!some_on_rocked)
            {
                try
                {
                    bottun_queue[bottun_y][bottun_x].on = true;
                    WritePage();
                    bottun_queue[bottun_y][bottun_x].FunctionExcute();
                    some_on_rocked = true;
                }
                catch (IndexOutOfRangeException ex)
                {
                    WritePage();
                    Console.WriteLine(ex.Message);
                }
                Wait();
            }
        }

        public void TurnOffToKeyEvent()
        {
            if (some_on_rocked && !dont_release)
            {
                try
                {
                    bottun_queue[bottun_y][bottun_x].on = false;
                    ChangeSelected();
                    some_on_rocked = false;
                }
                catch (IndexOutOfRangeException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                WritePage();
                Wait();
            }
        }

        public void ChangeSelected()
        {
            bottun_queue[old_y][old_x].selsected = false;
            if (bottun_queue[bottun_y][bottun_x].apear)
            {
                bottun_queue[bottun_y][bottun_x].selsected = true;
            }
        }

        private void XYGetOld()
        {
            old_x = bottun_x;
            old_y = bottun_y;
        }

        public void TurnOff()
        {
            try
            {
                bottun_queue[bottun_y][bottun_x].on = false;
                ChangeSelected();
                some_on_rocked = false;
            }
            catch (IndexOutOfRangeException ex)
            {
                Console.WriteLine(ex.Message);
            }
            WritePage();
            Wait();
        }

        public void BottunSelect()
        {
            while (!rocked)
            {
                Thread.Sleep(10); // CPU負荷軽減のために少しスリープ
            }
        }
    }
    public class Bottun
    {
        public int? console_top { get; set; } = null;

        public int x { get; set; }

        public int y { get; set; }
        public string rabel = "";
        public bool on { get; set; } = false;

        public bool selsected { get; set; } = false;
        public bool apear { get; set; } = true;

        public Action? Function { get; set; }
        public Bottun(int y, int x, string rabel = "bottun", bool apear = true)
        {
            this.x = x;
            this.y = y;
            this.rabel = rabel;
            this.apear = apear;
        }

        public void FuncInit(Action function)
        {
            this.Function = function;
        }

        public void FunctionExcute()
        {
            if (Function != null)
            {
                Function();
            }
        }

        public string Rabel()
        {
            if (apear)
            {
                if (on)
                {
                    return ">>" + rabel + "<<";
                }
                else if (selsected)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                    return "< " + rabel + " >";
                }
                else
                {
                    return "< " + rabel + " >";
                }
            }
            else
            {
                return "";
            }
        }
    }

    public class AnnouncementBox
    {
        private int height = 0;
        private List<StringBuilder> text_strage;
        private ConsoleBuffer cB;
        public AnnouncementBox(int height, ConsoleBuffer cB)
        {
            this.height = height;
            text_strage = new List<StringBuilder>();
            this.cB = cB;
            for (int i = 0; i < height; i++)
            {
                text_strage.Add(new StringBuilder());
            }

        }

        private void WriteBorder()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Console.BufferWidth; i++)
            {
                sb.Append("-");
            }
            cB.WriteLine(sb.ToString());
        }

        public void WriteLine()
        {
            int count = 0;
            WriteBorder();
            for (int i = 0; i < height; i++)
            {
                if (text_strage[i].ToString() != "")
                {
                    cB.WriteLine(text_strage[i].ToString());
                }
                else
                {
                    count++;
                }
            }
            for (int i = 0; i < count; i++)
            {
                cB.WriteLine("");
            }
            WriteBorder();
        }

        public void AddText(string str)
        {
            for (int i = 0; i < height - 1; i++)
            {
                text_strage[i].Clear();
                text_strage[i].Append(text_strage[i + 1].ToString());
            }
            text_strage[height - 1].Clear();
            text_strage[height - 1].Append(DateTime.Now.ToString("T") + " " + str);
        }
    }

    public class KeyEvnetSheet
    {
        public KeyEvnetSheet()
        {

        }


    }

    public class Program2
    {
        private static ConsoleBuffer consoleBuffer = new();
        private BottunSheet? BS;
        private int a = 0;
        private AnnouncementBox announcementBox = new(4, consoleBuffer);

        public void Process()
        {
            IKeyEvent keyEvent = new KeyEvent();
            RenderingForConsole renderingForConsole = new(keyEvent);
            
        }

        public void Write()
        {
            //Console.Clear();
            //Thread.Sleep(50);
            //フリッカー対策として、↓を導入。これでもちゃんと過不足なく描画できるように、自作バッファーをつくる
            //まず、ライトラインしたい文章を引数として要請できる、新ライトラインクラスを作って、そのくらすで要請ストリングをバッファーで切って改行したりしてライトラインする。このとき表がバグったりせんようにしなあかん。
            if (BS != null)
            {
                consoleBuffer.Clear();

                for (int i = 0; i < BS.BQ.Y_length(); i++)
                {
                    string str_to_write = "";
                    for (int ii = 0; ii < BS.BQ.X_length(i); ii++)
                    {
                        str_to_write = BS.BQ.GetRabel(i, ii);
                        if (BS.BQ.GetSelected(i, ii))
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.White;
                        }
                        consoleBuffer.Write(str_to_write);
                        if (Console.BackgroundColor == ConsoleColor.White)
                        {
                            Console.ResetColor();
                        }
                        consoleBuffer.Write(" ");

                    }
                    consoleBuffer.Write("bbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbc");
                    consoleBuffer.Write("bbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbc");
                    consoleBuffer.WriteLine("");
                }
                announcementBox.WriteLine();
                consoleBuffer.Write("test");

                consoleBuffer.ClearStop();
                consoleBuffer.WriteLine("(" + Console.GetCursorPosition().Left.ToString() + Console.GetCursorPosition().Top.ToString() + ")");

                //consoleBuffer.WriteLine("bbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbb");
                if (a == 1 || a % 2 == 0)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        //consoleBuffer.Write("ohh i love you when you like that and when you close up, give me the shiver. ohh baby you wanna dance till the sunlight crucks");
                        consoleBuffer.Write("bbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbc");
                    }
                }
                for (int i = 0; i < consoleBuffer.chars_in_line.Count; i++)
                {
                    consoleBuffer.Write(consoleBuffer.chars_in_line[i]);
                    consoleBuffer.Write(" ");
                }
                consoleBuffer.Write("test");
                consoleBuffer.FixCursolTop(25);
                //Thread.Sleep(3000);
                //consoleBuffer.WriteLine("hahaha");
                a++;
            }

        }

        public void Hoge()
        {
            consoleBuffer.Write("!!! ");
            consoleBuffer.Write(BS.BQ.X());
            consoleBuffer.Write(BS.BQ.Y());
            consoleBuffer.Write("!!! ");
            consoleBuffer.WriteLine("");
            BS.BQ.SetRabel(BS.BQ.Y(), BS.BQ.X(), "abc");
            announcementBox.AddText("ボタンが押されました");
        }
        public void Excute()
        {
            Action action = () => Write();
            Action actiona = () => Hoge();
            BS = new BottunSheet(action);
            string to_rabel = "bbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbbaaaaaaaaaabbbbbbbbbb";
            to_rabel = "bottun";
            BS.BQ.AddNewList(4, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(3, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(4, function: actiona, rabel: to_rabel);

            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: "↓↑");
            for (int i = 0; i < 10; i++)
            {
                BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            }
            /*
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
            */
            BS.StartHandlingKeyEvent();
            //var t1 = new Thread(BQ.Some);
            //t1.Start();
            //Console.WriteLine("hello world");

        }
    }
}

