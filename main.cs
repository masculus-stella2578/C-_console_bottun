using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestConsole1_2
{
    //---- 7/4メモ ----
    //それぞれのクラスの責任があやふやになってきてる。rockedのところとか特に。今一度どのクラスがどこまでの責任を担当して、それが最適なのかという確認、そして見直しを行う
    public class ConsoleBuffer
    {
        private int total_lines = 0;
        private int buffer_len;
        public ConsoleBuffer(int buffer_len)
        {
            this.buffer_len = buffer_len;
        }

        public void Clear()
        {

        }

        public void ClearStop()
        {

        }

        public void WriteLine(string str)
        {

        }

        public void Write(string str)
        {

        }
    }

    public class BottunSheet
    {
        public BottunQ BQ { get; set; } = new BottunQ();
        private Thread t1;
        public BottunSheet()
        {
            BQ = new BottunQ();
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

    public class BottunQ
    {
        private List<List<Bottun>> bottun_queue = new();
        private int bottun_x = 0;
        private int bottun_y = 0;
        private bool rocked { get; set; } = false;
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
        const int VK_SPACE = 0x20;
        private bool ctrlPressed = ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0);
        private bool sPressed = ((GetAsyncKeyState(VK_S) & 0x8000) != 0);
        private bool escPressed = ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0);
        private bool up_pressd = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
        private bool down_pressd = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
        private bool right_pressd = ((GetAsyncKeyState(VK_R) & 0x8000) != 0);
        private bool left_pressd = ((GetAsyncKeyState(VK_L) & 0x8000) != 0);
        private bool space_pressd = ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0);

        public BottunQ()
        {

        }

        public void AddBottun(int y = -1, int x = -1)
        {

        }

        public void AddNewList(int size_of_list, bool apeal = true, string rabel = "bottun")
        {
            if (0 < size_of_list)
            {
                bottun_queue.Add(new List<Bottun>());
                for (int i = 0; i < size_of_list; i++)
                {
                    bottun_queue[bottun_queue.Count - 1].Add(new Bottun(rabel, apeal));
                }
            }
            Console.WriteLine(bottun_queue.Count);
        }

        public void ChangeRabel(int x, int y, string rabel)
        {
            if ( ((0 <= y) && (y <= bottun_queue.Count - 1)) && ((0 <= x) && (x <= bottun_queue[y].Count - 1)) )
            {
                
            }
            else
            {
                Console.WriteLine("------ index error of bottun_queue ------");
            }
        }

        public void ReDefine()
        {
            up_pressd = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
            down_pressd = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
            right_pressd = ((GetAsyncKeyState(VK_R) & 0x8000) != 0);
            left_pressd = ((GetAsyncKeyState(VK_L) & 0x8000) != 0);
            space_pressd = ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0);
        }

        public void Wait()
        {
            while (up_pressd | down_pressd | right_pressd | left_pressd | space_pressd)
            {
                ReDefine();
                Thread.Sleep(10);
            }
        }

        public void Write()
        {
            //Console.Clear();
            //Thread.Sleep(50);
            //フリッカー対策として、↓を導入。これでもちゃんと過不足なく描画できるように、自作バッファーをつくる
            //まず、ライトラインしたい文章を引数として要請できる、新ライトラインクラスを作って、そのくらすで要請ストリングをバッファーで切って改行したりしてライトラインする。このとき表がバグったりせんようにしなあかん。
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < bottun_queue.Count; i++)
            {
                Console.WriteLine("");
                string str_to_write = "";
                for (int ii = 0; ii < bottun_queue[i].Count; ii++)
                {
                    str_to_write = bottun_queue[i][ii].Rabel();
                    Console.Write(str_to_write);
                    Console.Write(" ");
                }
            }
            if (a == 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    Console.Write("ohh i love you when you like that and when you close up, give me the shiver. ohh baby you wanna dance till the sunlight crucks");
                }
            }
            a = 1;
        }

        public void ChangeSelected()
        {
            if (bottun_queue[bottun_y][bottun_x].apear)
            {
                bottun_queue[bottun_y][bottun_x].selsected = true;
            }
        }

        public void BottunSelect()
        {
            Console.WriteLine(0 < bottun_y);
            Console.WriteLine(bottun_y < bottun_queue.Count);
            Console.WriteLine(bottun_x < bottun_queue[bottun_y].Count);
            Console.WriteLine((0 < bottun_x));
            while (!rocked)
            {
                ReDefine();
                if (up_pressd && (0 < bottun_y))
                {
                    if (bottun_x > bottun_queue[bottun_y - 1].Count - 1)
                    {
                        bottun_x = bottun_queue[bottun_y - 1].Count - 1;
                    }
                    bottun_y--;
                    ChangeSelected();
                    Write();
                    Wait();
                }
                else if (down_pressd && (bottun_y < bottun_queue.Count - 1))
                {
                    if (bottun_x > bottun_queue[bottun_y + 1].Count - 1)
                    {
                        bottun_x = bottun_queue[bottun_y + 1].Count - 1;
                    }
                    bottun_y++;
                    ChangeSelected();
                    Write();
                    Wait();
                }
                else if (right_pressd && (bottun_x < bottun_queue[bottun_y].Count - 1))
                {
                    bottun_x++;
                    ChangeSelected();
                    Write();
                    Wait();
                }
                else if (left_pressd && (0 < bottun_x))
                {
                    bottun_x--;
                    ChangeSelected();
                    Write();
                    Wait();
                }
                Thread.Sleep(10); // CPU負荷軽減のために少しスリープ
            }
        }
    }
    public class Bottun
    {
        public string rabel = "";
        public bool on { get; set; } = false;

        public bool selsected { get; set; } = false;
        public bool apear { get; set; }  = true;
        public Bottun(string rabel = "bottun", bool apear = true)
        {
            this.rabel = rabel;
            this.apear = apear;
        }
        
        public string Rabel()
        {
            if (apear)
            {
                if (on)
                {
                    return ">> " + rabel + " <<";
                }
                else if (selsected)
                {
                    selsected = false;
                    return "<< " + rabel + " >>";
                }
                else
                {
                    return "[  " + rabel + "  ]";
                }
            }
            else
            {
                return "";
            }
        }
    }
    public class Program
    {
        //f u and your mom and your sister and your job and your broken ass car and shit you car art 
        static void Main(string[] args)
        {
            BottunSheet BS = new BottunSheet();
            BS.BQ.AddNewList(4);
            BS.BQ.AddNewList(3);
            BS.BQ.AddNewList(2);
            BS.BQ.AddNewList(4 );
            BS.Strat();
            //var t1 = new Thread(BQ.Some);
            //t1.Start();
            Console.WriteLine("hello world");
        }
    }
}
