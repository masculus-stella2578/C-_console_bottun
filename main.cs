using System;
using System.Data;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using NPOI.HPSF;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace src
{
    //---- 7/4メモ ----
    //それぞれのクラスの責任があやふやになってきてる。rockedのところとか特に。今一度どのクラスがどこまでの責任を担当して、それが最適なのかという確認、そして見直しを行う


    //9/27メモ
    //いまconsol.writeline(str)としてるところをList<string>.append(str);に置き換えて最後にfor文で回して全部書く。
    //9/30
    //じゃなくて、先頭行と最終行のlineを変数としてもっとけば、そこからその範囲内に該当するところを随時描画していけばいい。ただ、何行描画されるか先に知ることがでいないから、どこまでも下に行けてまうんちゃう？

    internal interface IRenderingInConsole
    {
        public void StartHandlingKeyEvent();
        public void StopHandlingKeyEvent();
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

    //manage sheet queue and enteier using rendering for console
    public class RenderingForConsole
    {
        private List<Sheet> sheet_q;
        private Sheet? sheet_to_render;

        public RenderingForConsole()
        {
            sheet_q = new List<Sheet>();
            sheet_to_render = null;
        }

        public void AddSheet(Sheet? arg_sheet = null, int index = -1)
        {
            if (arg_sheet == null)
            {
                arg_sheet = new Sheet();
            }

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

            //スクリーンにまとめあげる
            //同時に差分を検知する行ごとに
            foreach (Section section in sheet_to_render.GetSections())
            {
                
            }

            //差分がある行のところだけを描画する.
            
        }

    }

    public interface IKeyEventHanler
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
    public class KeyEventHandler : IKeyEventHanler
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

    public class SheetQueue
    {

    }

    public class ElementRenderedOnConsole
    {

    }

    public class Sheet
    {
        public int serial_number { get; set; }

        private List<Section> sections;

        public Sheet()
        {
            sections = new List<Section>();
        }

        public void AddSection(Section? arg_section = null, int index = -1)
        {
            if (arg_section == null)
            {
                arg_section = new Section();
            }

            if (index < 0)
            {
                sections.Add(arg_section);
            }
            else
            {
                sections.Insert(index, arg_section);
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

    public class Section
    {
        public int serial_number { get; set; }
        public int line { get; set; }
        public int line_span { get; set; }
        public int pos { get; set; }
        public int pos_span { get; set; }

        private List<SectionLayer> layers;

        public Section()
        {
            layers = new List<SectionLayer>();
        }

        public void AddSection(SectionLayer? arg_layer = null, int index = -1)
        {
            if (arg_layer == null)
            {
                arg_layer = new SectionLayer();
            }

            if (index < 0)
            {
                layers.Add(arg_layer);
            }
            else
            {
                layers.Insert(index, arg_layer);
            }
        }

        public SectionLayer GetSection(int inndex)
        {
            return layers[inndex];
        }
    }

    public class SectionQueue
    {
        
    }

    public class SectionLayer
    {
        public int serial_number { get; set; }
        private List<StringBuilder> texts;
        private int total_line_num = 0;
        private int current_write_num = 0;

        public SectionLayer()
        {
            texts = new List<StringBuilder>();
        }

        public int GetLenOfTextsList()
        {
            return texts.Count;
        }

        public int GetTotalLineNnum()
        {
            return total_line_num;
        }

        public void Clear()
        {
            current_write_num = 0;
            total_line_num = 0;
        }

        public void WriteInLine(int line_num = -1)
        {
            if (line_num < 0)
            {

            }
            else
            {

            }
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

    public class BottunSheet : IRenderingInConsole
    {
        public BottunQ BQ { get; set; }
        private Thread t1;
        private Action WritePage;
        public BottunSheet(Action writePage)
        {
            WritePage = writePage;
            IKeyEventHanler keyEventHanler = new KeyEventHandler();
            BQ = new BottunQ(WritePage, keyEventHanler);
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

    }

    //ボタンを一つのシート上でキューとして管理。ここでは、ボタンの選択にかかわる処理や、キューに保持しているボタンの要素を変えたりすることができる。
    //キーボード操作はもう一つ独立してクラス作ったら？

    public class BottunQ
    {
        private IKeyEventHanler keyEventHanler;
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

        public BottunQ(Action writePage, IKeyEventHanler keyEventHanler)
        {
            WritePage = writePage;
            this.keyEventHanler = keyEventHanler;
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
            while (keyEventHanler.up_pressd | keyEventHanler.down_pressd | keyEventHanler.right_pressd | keyEventHanler.left_pressd | keyEventHanler.space_pressd)
            {
                Thread.Sleep(1);
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

        public void BottunSelect()
        {
            while (!rocked)
            {
                if (keyEventHanler.up_pressd && (0 < bottun_y) && !some_on_rocked)
                {
                    XYGetOld();
                    if (bottun_x > bottun_queue[bottun_y - 1].Count - 1)
                    {
                        bottun_x = bottun_queue[bottun_y - 1].Count - 1;
                    }
                    bottun_y--;
                    ChangeSelected();
                    WritePage();
                    Wait();
                }
                else if (keyEventHanler.down_pressd && (bottun_y < bottun_queue.Count - 1) && !some_on_rocked)
                {
                    XYGetOld();
                    if (bottun_x > bottun_queue[bottun_y + 1].Count - 1)
                    {
                        bottun_x = bottun_queue[bottun_y + 1].Count - 1;
                    }
                    bottun_y++;
                    ChangeSelected();
                    WritePage();
                    Wait();
                }
                else if (keyEventHanler.right_pressd && (bottun_x < bottun_queue[bottun_y].Count - 1) && !some_on_rocked)
                {
                    XYGetOld();
                    bottun_x++;
                    ChangeSelected();
                    WritePage();
                    Wait();
                }
                else if (keyEventHanler.left_pressd && (0 < bottun_x) && !some_on_rocked)
                {
                    XYGetOld();
                    bottun_x--;
                    ChangeSelected();
                    WritePage();
                    Wait();
                }
                else if (keyEventHanler.space_pressd && !some_on_rocked)
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
                else if (keyEventHanler.shift_pressed && keyEventHanler.space_pressd && some_on_rocked && !dont_release)
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
                Thread.Sleep(10); // CPU負荷軽減のために少しスリープ
            }
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

    public class Program2
    {
        private static ConsoleBuffer consoleBuffer = new();
        private BottunSheet? BS;
        private int a = 0;
        private AnnouncementBox announcementBox = new(4, consoleBuffer);

        public void Process()
        {
            RenderingForConsole renderingForConsole = new();
            
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
                if (a == 1 | a % 2 == 0)
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

