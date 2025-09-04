using System.Data;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestConsole1_2
{
    //---- 7/4メモ ----
    //それぞれのクラスの責任があやふやになってきてる。rockedのところとか特に。今一度どのクラスがどこまでの責任を担当して、それが最適なのかという確認、そして見直しを行う



    public class ConsoleBuffer
    {
        private bool write_before = false;
        static Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
        public int total_lines { get; private set; } = 1;
        private int old_tota_lines = 1;
        private int buffer_len = 0;
        private bool old_write_before;
        private bool clear_stop = false;
        private bool cursol_fixed = false;
        public List<int> chars_in_line { get; set; } = new();
        public List<int> old_chars_in_line { get; set; } = new();
        public List<bool> w_deleted = new();
        private int? top_to_fix = null;
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
            //繰り返しの記述を避けるためのローカル関数を以下に定義
            void WriteLinePakage()
            {
                Console.WriteLine("");
                total_lines++;
                CharsInLineAddSpace();
            }

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
        public void WriteLine(string args)
        {
            WriteLineProccess(args, true);
        }

        public void WriteLine(bool args)
        {
            WriteLineProccess(args.ToString(), true);
        }

        public void WriteLine(double args)
        {
            WriteLineProccess(args.ToString(), true);
        }

        public void WriteLine(int args)
        {
            WriteLineProccess(args.ToString(), true);
        }


        //以下、Writeの引数型によるオーバーライド
        public void Write(string args)
        {
            WriteLineProccess(args, false);
        }

        public void Write(bool args)
        {
            WriteLineProccess(args.ToString(), false);
        }

        public void Write(double args)
        {
            WriteLineProccess(args.ToString(), false);
        }

        public void Write(int args)
        {
            WriteLineProccess(args.ToString(), false);
        }
    }

    public class BottunSheetIndexer
    {

        public BottunSheetIndexer()
        {

        }

        
    }

    public class BottunSheet
    {
        public BottunQ BQ { get; set; }
        private Thread t1;
        private Action WritePage;
        public BottunSheet(Action writePage)
        {
            WritePage = writePage;
            BQ = new BottunQ(WritePage);
            t1 = new Thread(BQ.BottunSelect);
        }

        public void Strat()
        {
            t1.Start();
        }

        public void Stop()
        {
            t1.Join();
        }
    }

    //ボタンを一つのシート上でキューとして管理。ここでは、ボタンの選択にかかわる処理や、キューに保持しているボタンの要素を変えたりすることができる。
    //キーボード操作はもう一つ独立してクラス作ったら？
    public class BottunQ
    {
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
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private static int a = 0;

        const int VK_CONTROL = 0x11;
        const int VK_S = 0x53;
        const int VK_ESCAPE = 0x1B;
        const int VK_UP = 0x26;
        const int VK_DOWN = 0x28;
        const int VK_R = 0x27;
        const int VK_L = 0x25;
        const int VK_SPACE = 0x0D;
        const int VK_SHIFT = 0x10;
        private bool ctrlPressed = ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0);
        private bool sPressed = ((GetAsyncKeyState(VK_S) & 0x8000) != 0);
        private bool escPressed = ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0);
        private bool up_pressd = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
        private bool down_pressd = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
        private bool right_pressd = ((GetAsyncKeyState(VK_R) & 0x8000) != 0);
        private bool left_pressd = ((GetAsyncKeyState(VK_L) & 0x8000) != 0);
        private bool space_pressd = ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0);
        private bool shift_pressed = ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0);

        public BottunQ(Action writePage)
        {
            WritePage = writePage;
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

        public void AddNewList(int size_of_list,int y = -1, bool apeal = true, string rabel = "bottun", Action? function = null)
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
            if ( ((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)) )
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

        public void ReDefine()
        {
            up_pressd = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
            down_pressd = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
            right_pressd = ((GetAsyncKeyState(VK_R) & 0x8000) != 0);
            left_pressd = ((GetAsyncKeyState(VK_L) & 0x8000) != 0);
            space_pressd = ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0);
            shift_pressed = ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0);
        }

        public void Wait()
        {
            while (up_pressd | down_pressd | right_pressd | left_pressd | space_pressd)
            {
                ReDefine();
                Thread.Sleep(0);
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
                ReDefine();
                if (up_pressd && (0 < bottun_y) && !some_on_rocked)
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
                else if (down_pressd && (bottun_y < bottun_queue.Count - 1) && !some_on_rocked)
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
                else if (right_pressd && (bottun_x < bottun_queue[bottun_y].Count - 1) && !some_on_rocked)
                {
                    XYGetOld();
                    bottun_x++;
                    ChangeSelected();
                    WritePage();
                    Wait();
                }
                else if (left_pressd && (0 < bottun_x) && !some_on_rocked)
                {
                    XYGetOld();
                    bottun_x--;
                    ChangeSelected();
                    WritePage();
                    Wait();
                }
                else if (space_pressd && !some_on_rocked)
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
                else if (shift_pressed && space_pressd && some_on_rocked && !dont_release)
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
        public bool apear { get; set; }  = true;

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
            text_strage[height - 1].Append(DateTime.Now.ToString("T") +" " + str);
        }
    }
    public class Program
    {
        private static ConsoleBuffer consoleBuffer = new();
        private BottunSheet? BS;
        private int a = 0;
        private AnnouncementBox announcementBox = new(4, consoleBuffer);
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
            BS.Strat();
            //var t1 = new Thread(BQ.Some);
            //t1.Start();
            //Console.WriteLine("hello world");
        }
    }


    public class Hoge
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var pg = new Program();
            pg.Excute();
        }
    }
}
