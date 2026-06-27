using System;
using System.Data;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using Array = System.Array;
using Org.BouncyCastle.Bcpg.Sig;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ICSharpCode.SharpZipLib.Zip;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using NPOI.SS.Formula.Functions;
using NPOI.OpenXmlFormats.Spreadsheet;
using System.Windows.Forms;
using static NPOI.HSSF.Util.HSSFColor;
using System.Threading.Tasks;
using NPOI.HPSF;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using NPOI.SS.Util;

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

    internal class CSharpConsoleTUI
    {
        public RenderingClassForConsole renderingForConsole { get; private set; }
        public KeyEventHandler keyEventHandler { get; private set; }
        public IKeyEvent keyEvent { get; private set; }

        public CSharpConsoleTUI()
        {
            keyEvent = new KeyEvent();
            renderingForConsole = new(keyEvent);
            keyEventHandler = new(4, keyEvent);
        }
    }

    internal interface IKeyEvent
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public void ReDefine();
        public bool GetIsPressedArrayByIndex(KeyToIndex keyToIndex);
        public void Start();
        public void Stop();
    }

    //本番環境
    internal class KeyEvent : IKeyEvent
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        const int VK_CONTROL = 0x11;
        const int VK_S = 0x53;
        const int VK_ESCAPE = 0x1B;
        const int VK_UP = 0x26;
        const int VK_DOWN = 0x28;
        const int VK_RIGHT = 0x27;
        const int VK_LEFT = 0x25;
        const int VK_ENTER = 0x0D;
        const int VK_SHIFT = 0x10;
        const int VK_W = 0x57;
        const int VK_A = 0x41;
        const int VK_D = 0x44;
        const int VK_Q = 0x51;
        const int VK_E = 0x45;
        private bool[] Is_pressed_array_current = new bool[KeyEventHandlerElement.number_of_available_key];
        private bool[] Is_pressed_array_next = new bool[KeyEventHandlerElement.number_of_available_key];
        private bool stop_process = false;
        private CancellationTokenSource? _cts;

        public KeyEvent()
        {

        }

        public void ReDefine()
        {
            Is_pressed_array_next[(int)KeyToIndex.Up] = ((GetAsyncKeyState(VK_UP) & 0x8000) != 0);
            Is_pressed_array_next[(int)KeyToIndex.Down] = ((GetAsyncKeyState(VK_DOWN) & 0x8000) != 0);
            Is_pressed_array_next[(int)KeyToIndex.Right] = ((GetAsyncKeyState(VK_RIGHT) & 0x8000) != 0);
            Is_pressed_array_next[(int)KeyToIndex.Left] = ((GetAsyncKeyState(VK_LEFT) & 0x8000) != 0);
            Is_pressed_array_next[(int)KeyToIndex.Enter] = ((GetAsyncKeyState(VK_ENTER) & 0x8000) != 0);
            Is_pressed_array_next[(int)KeyToIndex.Shift] = ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0);
            Is_pressed_array_next[(int)KeyToIndex.W] = (GetAsyncKeyState(VK_W) & 0x8000) != 0;
            Is_pressed_array_next[(int)KeyToIndex.A] = (GetAsyncKeyState(VK_A) & 0x8000) != 0;
            Is_pressed_array_next[(int)KeyToIndex.S] = (GetAsyncKeyState(VK_S) & 0x8000) != 0;
            Is_pressed_array_next[(int)KeyToIndex.D] = (GetAsyncKeyState(VK_D) & 0x8000) != 0;
            Is_pressed_array_next[(int)KeyToIndex.Q] = (GetAsyncKeyState(VK_Q) & 0x8000) != 0;
            Is_pressed_array_next[(int)KeyToIndex.E] = (GetAsyncKeyState(VK_E) & 0x8000) != 0;
            Is_pressed_array_next[(int)KeyToIndex.Shift_Q] = Is_pressed_array_next[(int)KeyToIndex.Shift] && Is_pressed_array_next[(int)KeyToIndex.Q];
            Is_pressed_array_next[(int)KeyToIndex.Shift_E] = Is_pressed_array_next[(int)KeyToIndex.Shift] && Is_pressed_array_next[(int)KeyToIndex.E];
            Is_pressed_array_next[(int)KeyToIndex.Shift_W] = Is_pressed_array_next[(int)KeyToIndex.Shift] && Is_pressed_array_next[(int)KeyToIndex.W];
            Is_pressed_array_next[(int)KeyToIndex.Shift_A] = Is_pressed_array_next[(int)KeyToIndex.Shift] && Is_pressed_array_next[(int)KeyToIndex.A];
            Is_pressed_array_next[(int)KeyToIndex.Shift_S] = Is_pressed_array_next[(int)KeyToIndex.Shift] && Is_pressed_array_next[(int)KeyToIndex.S];
            Is_pressed_array_next[(int)KeyToIndex.Shift_D] = Is_pressed_array_next[(int)KeyToIndex.Shift] && Is_pressed_array_next[(int)KeyToIndex.D];
        }

        private void Process(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ReDefine();
                var temp = Is_pressed_array_current;
                Is_pressed_array_current = Is_pressed_array_next;
                Is_pressed_array_next = temp;
                Thread.Sleep(10);
            }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => Process(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public bool GetIsPressedArrayByIndex(KeyToIndex keyToIndex)
        {
            return Is_pressed_array_current[(int)keyToIndex];
        }
    }

    internal class KeyEventHandlerElement
    {
        internal const int number_of_available_key = 18;
    }

    internal enum KeyToIndex
    {
        Up,
        Down,
        Left,
        Right,
        Shift,
        Enter,
        W,
        S,
        A,
        D,
        Q,
        E,
        Shift_W,
        Shift_S,
        Shift_A,
        Shift_D,
        Shift_Q,
        Shift_E,
    }

    internal class KeyEventHandlerOneThread : KeyEventHandlerElement
    {

        private List<Action?>[] actions = new List<Action?>[number_of_available_key];
        private IKeyEvent keyEvent;
        private CancellationTokenSource? _cts;
        public int serial_number { get; set; }
        public KeyEventHandlerOneThread(IKeyEvent keyEvent)
        {
            this.keyEvent = keyEvent;
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i] = new List<Action?>();
            }
        }

        public void SetOneKeyAction(int index, List<Action?>? arg_actions)
        {
            if (arg_actions == null)
                return;

            foreach (Action? action in arg_actions)
            {
                if (action != null)
                {
                    actions[index].Add(action);
                }
            }
        }

        public void ExecuteActions(KeyToIndex keyToIndex)
        {
            //Console.WriteLine(keyToIndex);
            //Console.WriteLine(serial_number);

            if (actions[(int)keyToIndex] == null)
            {
                //Console.WriteLine("b");
                return;
            }
            foreach (Action? action in actions[(int)keyToIndex])
            {
                //Console.WriteLine("c");
                if (action != null)
                    action();
            }
        }

        public async void KeyEventHandl(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < KeyEventHandlerElement.number_of_available_key; i++)
                {
                    //Console.WriteLine("!!" + serial_number + "!!");

                    if (keyEvent.GetIsPressedArrayByIndex((KeyToIndex)i))
                    {
                        ExecuteActions((KeyToIndex)i);
                    }
                }
                //cpu負荷軽減.
                Thread.Sleep(10);

                //Console.WriteLine("??"+serial_number+"??");
            }
        }

        public void Clear()
        {
            Array.Clear(actions, 0, actions.Length);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => KeyEventHandl(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }
    }

    internal class KeyEventHandler : KeyEventHandlerElement
    {
        private List<KeyEventHandlerOneThread> thread_list = new();
        private int[] is_already_added_action = new int[number_of_available_key];
        private int mux_index_of_thread = 0;
        private int max_thread_num = 0;
        private int serial_for_thread = 0;
        private IKeyEvent keyEvent;
        public KeyEventHandler(int max_thread_num, IKeyEvent keyEvent)
        {
            this.max_thread_num = max_thread_num;
            this.keyEvent = keyEvent;
        }

        public void AddThread()
        {
            var t = new KeyEventHandlerOneThread(keyEvent);
            t.serial_number = serial_for_thread++;
            thread_list.Add(t);
        }

        public KeyEventHandler AddAction(int thread_index, KeyToIndex key_to_index, params Action[] actions)
        {
            var list = new List<Action>(actions);
            thread_list[thread_index].SetOneKeyAction((int)key_to_index, list);
            return this;
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


    internal interface IInitRenderingElement
    {
        public void Init();
    }

    internal class ApplicableSectionListInLine
    {
        public List<List<SectionInfoInLine>> SectionLists { get; private set; } = new();
    }

    internal class ResultCharInfo
    {
        public StringBuilder stringBuilder = new StringBuilder();
    }

    internal class ColorManegementInCharBufferContext
    {
        public TUIColorEnum _color1 { get; private set; } = TUIColorEnum.None;
        public TUIColorEnum _color2 { get; private set; } = TUIColorEnum.None;
        TUIColorEnum _color_to_one_space = TUIColorEnum.None;
        StringBuilder stringBuilder = new StringBuilder();
        private Dictionary<TUIColorEnum, string> keyValuePairs = new Dictionary<TUIColorEnum, string>
        {   {TUIColorEnum.None, TUIColorString.Reset},
            {TUIColorEnum.BlackLetter, TUIColorString.BlackLetter },
            {TUIColorEnum.WriteLetter, TUIColorString.WriteLetter },
            {TUIColorEnum.GreenLetter, TUIColorString.GreenLetter },
            {TUIColorEnum.RedLetter, TUIColorString.RedLetter },
            {TUIColorEnum.WriteBack, TUIColorString.WriteBack}
        };

        public ColorManegementInCharBufferContext()
        {

        }

        public void ReStartFromBeginning()
        {
            _color1 = TUIColorEnum.None;
            _color1 = TUIColorEnum.None;
        }

        public StringBuilder ReturnColorStrToWriteChar(TUIColorEnum color_arg1, TUIColorEnum color_arg2)
        {
            //noneじゃない、前と同じじゃない、存在するなら

            //Console.WriteLine("color management");
            //Console.WriteLine("color_arg : " +  color_arg1 + ", " + color_arg2 );
            //Console.WriteLine("color1, 2 : " + _color1 + ", " + _color2);

            stringBuilder.Clear();
            if (color_arg1 != _color1 &&
                keyValuePairs.TryGetValue(color_arg1, out string? color1_str))
            {
                //Console.WriteLine("one is approved");
                stringBuilder.Append(color1_str);
                _color_to_one_space = color_arg1;
            }
            _color1 = color_arg1;
            if ((color_arg2 != _color1 && color_arg2 != _color2) &&
                keyValuePairs.TryGetValue(color_arg2, out string? color2_str))
            {
                //Console.WriteLine("two is approved");
                stringBuilder.Append(color2_str);
                _color_to_one_space = color_arg2;
            }
            _color2 = color_arg2;
            return stringBuilder;
        }

        public StringBuilder ReturnColorStrToWriteOneSpace()
        {
            //もし前に何かあったら、もしくはResetされていなかったら
            if (_color_to_one_space != TUIColorEnum.None)
            {
                stringBuilder.Append(TUIColorString.Reset);
            }
            _color2 = TUIColorEnum.None;
            return stringBuilder;
        }
    }

    internal class CharBufferContext
    {
        private List<StringBuilder>? text_to_write = null;
        public Sheet? sheet_to_render { get; private set; } = null;
        public int Console_y { get; private set; } = 0;
        public int Console_x { get; set; } = 0;
        public int Last_console_y { get; private set;} = 0;

        private int previous_console_x_len = 0;
        private int previous_console_y_len = 0;

        private CharBufferXContext charBufferXContext;
        private ApplicableSectionListInLineContext applicableSectionContext;
        private CharBufferXBySectionContext charBufferXBySectionContext;

        public CharBufferContext()
        {
            charBufferXBySectionContext = new CharBufferXBySectionContext(this);

            applicableSectionContext = new ApplicableSectionListInLineContext(this);
            charBufferXContext = new CharBufferXContext(this, applicableSectionContext, charBufferXBySectionContext);
        }

        private void MakeUpTheRestOfSpaceCore(int times)
        {
            if (charBufferXBySectionContext.GetColor1() != TUIColorEnum.None && charBufferXBySectionContext.GetColor2() != TUIColorEnum.None)
            {
                text_to_write[Console_y].Append(TUIColorString.Reset);
            }
            text_to_write[Console_y].Append('\\', times);
        }

        private void MakeUpTheRestOfSpaceForSection(int console_width)
        {
            if (charBufferXContext.charBufferBySectionContex.section_to_render == null ||
                Console_x + charBufferXContext.charBufferBySectionContex.section_to_render.X_span - charBufferXContext.charBufferBySectionContex.how_many_chars_did_write > console_width
                )
            {
                MakeUpTheRestOfSpaceForConsole(console_width);
            }
            else
            {
                MakeUpTheRestOfSpaceCore(charBufferXContext.charBufferBySectionContex.section_to_render.X_span - charBufferXContext.charBufferBySectionContex.how_many_chars_did_write);
            }
        }

        private void MakeUpTheRestOfSpaceForConsole(int console_width)
        {
            //Console.WriteLine("-------------------------------");
            //Console.WriteLine("Console_y : " + Console_y + ", Console_x : "+ Console_x + ", console_width : " + console_width);
            //Console.WriteLine("-------------------------------");
            MakeUpTheRestOfSpaceCore(console_width - Console_x);
        }

        private bool IsNoSectionChangedInALine(int text_to_write_y, int console_width)
        {
            //変更されているかされていないか判定.
            //されていたら次のコンソール行へ.
            //このyに入っているsectionを回して、全てのセクションが変更なし、か、一つでも変更有かどうかを調べる.
            bool no_section_is_changed = true;

            //Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< " + text_to_write_y);
            //Console.WriteLine(text_to_write_y);
            //Console.WriteLine(sheet_to_render.applicableSectionListinLine.SectionLists.Count);
            //Console.WriteLine(console_width);

            foreach (SectionInfoInLine sectionInfoInLine in sheet_to_render.applicableSectionListinLine.SectionLists[text_to_write_y])
            {
                Section section = sheet_to_render.GetSection(sectionInfoInLine.section_serial_num);
                int section_y = section.Page_starting_y_pos + sectionInfoInLine.line_serial;

                //layerを回す.
                bool no_layer_is_changed = true;
                foreach (SectionLayer layer in section.layers)
                {
                    if (layer.texts_info.Count - 1 < section_y)
                        continue;
                    if (layer.texts_info[section_y].Is_changed)
                    {
                        //Console.WriteLine("IsNoSectionChanched - 1");
                        no_layer_is_changed = false;
                        break;
                    }
                }

                if (no_layer_is_changed == false)
                {
                    //Console.WriteLine("IsNoSectionChanched - 2");
                    no_section_is_changed = false;
                    break;
                }
            }
            //Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

            return no_section_is_changed;
        }

        public void InitWhenSheetWasSeted()
        {
            applicableSectionContext.InitWhenSheetWasSeted(sheet_to_render.applicableSectionListinLine);
        }

        internal void Integration(Sheet arg_sheet, List<StringBuilder> arg_text_to_write, int console_y_length, int console_x_length)
        {
            bool is_first_time = true;
            bool console_len_is_changed = (previous_console_x_len != console_x_length || previous_console_y_len != console_y_length);

            sheet_to_render = arg_sheet;
            text_to_write = arg_text_to_write;
            InitWhenSheetWasSeted();

            CharBufferContextResult charBufferContextResult = new CharBufferContextResult();
            bool up_to_date = false;
            Last_console_y = 0;
            Console_y = -1;

            while (true)
            {
                //Console.WriteLine("console : " + Console_x + ", " + Console_y);
                //Console.Write(charBufferContextResult.Go_to_next_y + ", ");

                //セクションごとののresult
                CharBufferBySectionContextResult? resultBySection = charBufferContextResult.charBufferBySectionContextResult;
                if (resultBySection != null)
                {
                    //Console.Write(resultBySection.stringBuilder + ", ");
                    //Console.WriteLine(resultBySection.Make_up_the_rest_of_space);
                    //Console.WriteLine("y - 1");
                    if (resultBySection.stringBuilder.Length != 0)
                    {
                        //Console.WriteLine("y - 2 : " + resultBySection.stringBuilder);
                        text_to_write[Console_y].Append(resultBySection.stringBuilder);
                    }
                    if (resultBySection.Make_up_the_rest_of_space)
                    {
                        //Console.WriteLine("y - 3");
                        MakeUpTheRestOfSpaceForSection(console_x_length);
                    }
                }
                else
                {
                    //Console.WriteLine("null");
                }

                //x全体のresult
                if (charBufferContextResult.Make_up_the_rest_of_space)
                {
                    //Console.WriteLine("y - 4");
                    MakeUpTheRestOfSpaceForConsole(console_x_length);
                }

                up_to_date = false;
                if (charBufferContextResult.Go_to_next_y || Console_x > console_x_length - 1 || is_first_time)
                {
                    //Console.WriteLine("go_to_next_line");
                    //Console.WriteLine("y - 5");
                    Console_x = 0;
                    while (true)
                    {
                        //Console.WriteLine("y - 6 : " + Console_y);
                        up_to_date = true;
                        Console_y++;
                        if (Console_y > console_y_length - 1)
                        {
                            break;
                        }

                        //Console.WriteLine("================================");
                        //Console.WriteLine(console_len_is_changed + ", " + (IsNoSectionChangedInALine(Console_y, console_y_length) == false));
                        //Console.WriteLine("================================");

                        if (console_len_is_changed || false == IsNoSectionChangedInALine(Console_y, console_y_length))
                        {
                            //Console.WriteLine("clearing");

                            text_to_write[Console_y].Clear();
                            Last_console_y = Console_y;
                            break;
                        }
                    }
                }

                //コンソール超えそうやったら終わる.
                if (Console_y > console_y_length - 1)
                {
                    //Console.WriteLine("y - 7");
                    break;
                }


                //Console.WriteLine("y - 8");
                if (is_first_time)
                {
                    up_to_date = true;
                }

                charBufferContextResult = charBufferXContext.Consume(up_to_date, Console_y);
                is_first_time = false;
            }

            previous_console_x_len = console_x_length;
            previous_console_y_len = console_y_length;
        }
    }

    internal class ApplicableSectionResult
    {
        public SectionInfoInLine? sectionInfoInLine = null;
        public bool Is_end_this_line = false;
    }

    internal class ApplicableSectionListInLineContext
    {
        private ApplicableSectionListInLine? applicableSectionListInLine = null;
        private int applicable_sections_index = 0;
        private int applicable_sections_y = 0;

        private ApplicableSectionResult result_in_aline = new();

        private CharBufferContext charBufferContext;
        public ApplicableSectionListInLineContext(CharBufferContext charBufferContext)
        {
            this.charBufferContext = charBufferContext;
        }

        public void InitWhenSheetWasSeted(ApplicableSectionListInLine arg_applicableSectionListInLine)
        {
            applicableSectionListInLine = arg_applicableSectionListInLine;
        }

        internal ApplicableSectionResult ResetFromThisYLineBeggining()
        {
            applicable_sections_index = 0;
            if (charBufferContext.Console_y > applicableSectionListInLine.SectionLists.Count - 1)
            {
                result_in_aline.Is_end_this_line = true;
            }
            else
            {
                result_in_aline.Is_end_this_line = false;
            }
            return result_in_aline;
        }

        internal ApplicableSectionResult ConsumeSection()
        {
            if (applicable_sections_index > applicableSectionListInLine.SectionLists[charBufferContext.Console_y].Count - 1)
            {
                result_in_aline.sectionInfoInLine = null;
                result_in_aline.Is_end_this_line = true;
                return result_in_aline;
            }
            else
            {
                result_in_aline.sectionInfoInLine = applicableSectionListInLine.SectionLists[charBufferContext.Console_y][applicable_sections_index];
                result_in_aline.Is_end_this_line = false;
            }

            applicable_sections_index++;

            return result_in_aline;
        }
    }

    internal class CharBufferContextResult
    {
        public bool Go_to_next_y = false;
        public bool Make_up_the_rest_of_space = false;
        public CharBufferBySectionContextResult? charBufferBySectionContextResult;
    }

    internal class CharBufferXContext
    {
        private CharBufferContext charBufferContext;
        private ApplicableSectionListInLineContext applicableSectionContext;
        public CharBufferXBySectionContext charBufferBySectionContex { get; private set; }
        private CharBufferContextResult result_to_return = new();
        public CharBufferXContext(CharBufferContext charBufferContext, ApplicableSectionListInLineContext applicableSectionContext, CharBufferXBySectionContext charBufferBySectionContex)
        {
            this.charBufferContext = charBufferContext;
            this.applicableSectionContext = applicableSectionContext;
            this.charBufferBySectionContex = charBufferBySectionContex;
        }

        private void ClearResult()
        {
            result_to_return.Go_to_next_y = false;
            result_to_return.Make_up_the_rest_of_space = false;
            result_to_return.charBufferBySectionContextResult = null;
        }

        private CharBufferContextResult SetResult(bool go_to_next_y = false, bool make_up_the_rest_of_space = false, CharBufferBySectionContextResult? result = null)
        {
            result_to_return.Go_to_next_y = go_to_next_y;
            result_to_return.Make_up_the_rest_of_space = make_up_the_rest_of_space;
            result_to_return.charBufferBySectionContextResult = result;
            return result_to_return;
        }

        public void InitWhenSheetWasSeted()
        {

        }

        internal CharBufferContextResult Consume(bool do_up_tp_date_y, int? console_y = null)
        {
            ClearResult();

            if (do_up_tp_date_y)
            {
                //Console.WriteLine("appli - 0");
                ApplicableSectionResult appli_result = applicableSectionContext.ResetFromThisYLineBeggining();
                if (appli_result.Is_end_this_line)
                {
                    //Console.WriteLine("appli - 1");
                    return SetResult(
                        go_to_next_y: true,
                        make_up_the_rest_of_space: true,
                        result: null);
                }
            }

            //Console.WriteLine("appli - 2");
            return Consume(do_up_tp_date_y);
        }

        internal CharBufferContextResult Consume(bool be_done_up_tp_date)
        {
            CharBufferBySectionContextResult charBufferBySectionContextResult = new();
            bool up_to_date = false;
            Section? section_to_render = null;
            int? section_x = null;
            int? section_y = null;
            while (true)
            {
                if (charBufferBySectionContextResult.Go_to_next_section || be_done_up_tp_date)
                {
                    //Console.WriteLine("appli - 3");
                    ApplicableSectionResult appli_result2 = applicableSectionContext.ConsumeSection();
                    if (appli_result2.Is_end_this_line)
                    {
                        //Console.WriteLine("appli - 4");
                        return SetResult(
                            go_to_next_y: true,
                            make_up_the_rest_of_space: true,
                            result: null);
                    }
                    else
                    {
                        //Console.WriteLine("appli - 5");
                        up_to_date = true;
                        section_to_render = charBufferContext.sheet_to_render.GetSection(appli_result2.sectionInfoInLine.section_serial_num);
                        section_y = section_to_render.Page_starting_y_pos + appli_result2.sectionInfoInLine.line_serial;
                        section_x = section_to_render.Page_starting_x_pos;
                    }
                }

                charBufferBySectionContextResult = charBufferBySectionContex.Consume(up_to_date, section_to_render, section_x, section_y);
                if (!charBufferBySectionContextResult.Go_to_next_section)
                {
                    //Console.WriteLine("appli - 6");
                    if (charBufferBySectionContextResult != null)
                    {
                        //Console.WriteLine(charBufferBySectionContextResult.stringBuilder);
                        //Console.WriteLine(charBufferBySectionContextResult.Go_to_next_section);
                        //Console.WriteLine(charBufferBySectionContextResult.Make_up_the_rest_of_space);
                    }
                    else
                    {
                        //Console.WriteLine("null");
                    }
                    return SetResult(
                    go_to_next_y: false,
                    make_up_the_rest_of_space: false,
                    result: charBufferBySectionContextResult);
                }
            }
        }
    }

    internal class CharBufferBySectionContextResult
    {
        public bool Go_to_next_section = false;
        public bool Make_up_the_rest_of_space = false;
        public StringBuilder stringBuilder = new();
    }

    internal class CharBufferXBySectionContext
    {
        private CharBufferContext charBufferContext;
        private ColorManegementInCharBufferContext colorManegementInCharBufferContext = new();

        public int Section_x { get; private set; } = 0;
        public int Section_y { get; private set; } = 0;
        public int how_many_chars_did_write { get; private set; } = 0;
        private int Max_layar_length_in_section = 0;
        private CharBufferBySectionContextResult result = new();
        public Section? section_to_render { get; private set; } = null;

        public CharBufferXBySectionContext(CharBufferContext charBufferContext)
        {
            this.charBufferContext = charBufferContext;
        }

        public TUIColorEnum GetColor1()
        {
            return colorManegementInCharBufferContext._color1;
        }

        public TUIColorEnum GetColor2()
        {
            return colorManegementInCharBufferContext._color2;
        }

        private void ClearResult()
        {
            result.Go_to_next_section = false;
            result.Make_up_the_rest_of_space = false;
            result.stringBuilder.Clear();
        }

        private StringBuilder SetResult(bool go_to_next_section = false, bool make_up_the_rest_of_space = false)
        {
            result.Go_to_next_section = go_to_next_section;
            result.Make_up_the_rest_of_space = make_up_the_rest_of_space;
            return result.stringBuilder;
        }

        public void InitWhenSheetWasSeted()
        {

        }

        private int CalcuMaxLayarLengthInSection()
        {
            int Max_layar_length_in_section = 0;
            foreach (SectionLayer sectionLayer in section_to_render.layers)
            {
                if (sectionLayer.Total_writed_line_count > Section_y && sectionLayer.texts_info[Section_y].length_in_English > Max_layar_length_in_section)
                {
                    Max_layar_length_in_section = sectionLayer.texts_info[Section_y].length_in_English;
                }
            }
            return Max_layar_length_in_section;
        }

        internal CharBufferBySectionContextResult Consume(bool do_up_tp_date_section, Section? section = null, int? section_x = null, int? section_y = null)
        {
            ClearResult();
            if (do_up_tp_date_section)
            {
                //Console.WriteLine("ConsumeTotalLine - 1");
                section_to_render = section;
                Section_x = (int)section_x;
                Section_y = (int)section_y;
                if (Section_y > section_to_render.Total_writed_line_count - 1)
                {
                    //Console.WriteLine("ConsumeTotalLine - 2");
                    SetResult(go_to_next_section: true, make_up_the_rest_of_space: true);
                    return result;
                }
                else
                {
                    Max_layar_length_in_section = CalcuMaxLayarLengthInSection();
                }
            }

            //Console.WriteLine("ConsumeTotalLine - 3");
            return ConsumeCharInCurrentY();
        }

        private CharBufferBySectionContextResult ConsumeCharInCurrentY()
        {
            //Console.WriteLine("ConsumeCharInCurrentY - 1");
            //Console.Write("section_x : " + Section_x);
            //Console.Write(", opposed : " + section_to_render.Length_in_English_List[Section_y]);

            if (Section_x > Max_layar_length_in_section - 1)
            {
                //Console.WriteLine("consume char in current y");
                //Console.WriteLine("ConsumeCharInCurrentY - 1");

                SetResult(
                    go_to_next_section: true,
                    make_up_the_rest_of_space: true);
                return result;
            }

            SectionLayer layer;
            CharType charType;
            SectionCharInfo sectionCharInfo;

            //後ろからlayerを回す.
            for (int i = 0, layer_index = section_to_render.layers.Count - 1; i < section_to_render.layers.Count; i++, layer_index--)
            {
                //Console.WriteLine("ConsumeCharInCurrentY - 2");

                layer = section_to_render.GetSectionLayer(layer_index);

                //このlayerにx, yに文字がなかったら(lengthからオーバーしてたら)continu(section.section_layer.texts_infoのインデックスアクセスの安全装置)
                //必ず少なくとも一つのlayerに文字がある(上のifがあることによって範囲内のx, yしか受け付けないから)
                if (Section_y > layer.Total_writed_line_count - 1 || Section_x > layer.texts_info[Section_y].length_in_English - 1)
                {
                    //Console.WriteLine("skip layer for");
                    continue;
                }

                //Console.WriteLine("skip skip");

                sectionCharInfo = layer.texts_info[Section_y].char_info_list[Section_x];
                charType = sectionCharInfo.type;

                //Console.WriteLine(sectionCharInfo.type);
                //Console.WriteLine("\'" + sectionCharInfo.charactor + "\'");

                //Console.WriteLine("==================================");
                //Console.WriteLine(charBufferContext.Console_x + ", " + charBufferContext.Console_y);
                switch (charType)
                {
                    case CharType.Empty:
                        if (layer_index == 0)
                        {
                            SetResult().Append(
                                colorManegementInCharBufferContext.ReturnColorStrToWriteOneSpace());

                            result.stringBuilder.Append(' ');

                            Section_x++;
                            charBufferContext.Console_x++;
                            how_many_chars_did_write++;
                        }
                        continue;

                    case CharType.Singular:
                        SetResult().Append(
                            colorManegementInCharBufferContext.ReturnColorStrToWriteChar(sectionCharInfo.color_arg1, sectionCharInfo.color_arg2)
                        );

                        SetResult().Append(sectionCharInfo.charactor);
                        //Console.WriteLine("singular \'" + sectionCharInfo.charactor + "\'" + charBufferContext.Console_x + "," + charBufferContext.Console_y);

                        Section_x++;
                        charBufferContext.Console_x++;
                        how_many_chars_did_write++;
                        break;

                    case CharType.PluralStart:
                        if (Section_x + 1 > section_to_render.Page_starting_x_pos + section_to_render.X_span)
                        {
                            SetResult().Append(
                                colorManegementInCharBufferContext.ReturnColorStrToWriteOneSpace());

                            SetResult().Append(' ');

                            Section_x++;
                        }
                        else
                        {
                            //    い.
                            //   あ
                            SetResult().Append(
                                colorManegementInCharBufferContext.ReturnColorStrToWriteChar(sectionCharInfo.color_arg1, sectionCharInfo.color_arg2)
                            );
                            SetResult().Append(sectionCharInfo.charactor);

                            Section_x += 2;
                            charBufferContext.Console_x += 2;
                            how_many_chars_did_write += 2;
                        }
                        break;

                    default:
                        //Console.WriteLine("default \'" + sectionCharInfo.charactor + "\'");
                        break;
                }
            }

            return result;
        }
    }

    internal class RenderingClassForConsole
    {
        private List<Sheet> sheet_q;
        private Sheet? sheet_to_render;
        private IKeyEvent keyEvent;
        private List<StringBuilder> text_to_write = new();
        private StringBuilder fainal_sb_to_write = new();
        public int console_y_length { get; private set; }
        public int console_x_length { get; private set; }
        private int sheet_serial_num = 0;
        private CharBufferContext charBufferContext;
        private ResultCharInfo? resultCharInfo;

        public RenderingClassForConsole(IKeyEvent keyEvent)
        {
            sheet_q = new List<Sheet>();
            sheet_to_render = null;
            this.keyEvent = keyEvent;
            charBufferContext = new CharBufferContext();
            SetConsoleYLengthToConsoleHeight();
        }

        public void Init()
        {
            SetConsoleYLengthToConsoleHeight();
        }

        public void SetConsoleYLengthToConsoleHeight()
        {
            int to = Console.WindowHeight;
            console_x_length = Console.WindowWidth;
            console_y_length = to;

            while (console_y_length > text_to_write.Count)
            {
                text_to_write.Add(new StringBuilder());
            }

            foreach (Sheet sheet in sheet_q)
            {
                sheet.SetApplicableSectionsInLineCount(to);
            }
        }

        public void AddSheet(Sheet? arg_sheet = null, int index = -1)
        {
            if (arg_sheet == null)
            {
                arg_sheet = new Sheet(keyEvent, this);
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

        public void AddSheet(int times)
        {
            for (int i = 0; i < times; i++)
            {
                Sheet sheet = new Sheet(keyEvent, this);
                sheet.SetApplicableSectionsInLineCount(console_y_length);
                sheet_q.Add(sheet);
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

        private int count = 0;

        public void RenderingOnConsole()
        {
            count++;
            //Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            if (count == 3)
            {
                //Environment.Exit(0);
            }
            //Console.Clear();

            charBufferContext.Integration(sheet_to_render, text_to_write, console_y_length, console_x_length);

            
            /*for (int i = 0; i < sheet_to_render.GetSection(0).GetSectionLayer(0).texts_info.Count; i++)
            {
                for (int ii = 0; ii < sheet_to_render.GetSection(0).GetSectionLayer(0).texts_info[i].char_info_list.Count; ii++)
                {
                    Console.Write("(" + sheet_to_render.GetSection(0).GetSectionLayer(0).texts_info[i].char_info_list[ii].color_arg1 + ", " +
                        sheet_to_render.GetSection(0).GetSectionLayer(0).texts_info[i].char_info_list[ii].color_arg2 + ")");
                    Console.WriteLine(" " + sheet_to_render.GetSection(0).GetSectionLayer(0).texts_info[i].char_info_list[ii].charactor);
                }
            }
            */
            



            //できたtexts_to_writeを一つのStringBuilderにして描画する.
            fainal_sb_to_write.Clear();
            for (int i = 0; i < console_y_length; i++)
            {
                if (text_to_write.Count - 1 < i)
                {
                    break;
                }
                fainal_sb_to_write.Append(text_to_write[i]);

                //if (charBufferContext.Last_console_y == i)
                //{
                //    fainal_sb_to_write.Append(TUIColorString.Reset);
                //}

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
        public RenderingClassForConsole parents_render_class { get; private set; }
        public ApplicableSectionListInLine applicableSectionListinLine { get; private set; } = new();
        private List<int> section_serial_number_s = new();
        private List<int> section_x_pos_list = new();
        private List<SectionSerialNumberAndXpos> serial_and_xpos = new();
        private IKeyEvent keyEvent;

        public Sheet(IKeyEvent keyEvent, RenderingClassForConsole parents_render_class)
        {
            sections = new List<Section>();
            this.keyEvent = keyEvent;
            this.parents_render_class = parents_render_class;
        }

        public void SetApplicableSectionsInLineCount(int to_this_length)
        {
            while (to_this_length > applicableSectionListinLine.SectionLists.Count)
            {
                applicableSectionListinLine.SectionLists.Add(new List<SectionInfoInLine>());
            }
            ResetSectionsInfoInLine(false);
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

        public ApplicableSectionListInLine GetSectionsInLine()
        {
            return applicableSectionListinLine;
        }

        public void AddSection(int x_pos, int x_span, int y_pos, int y_span, int index = -1, Section? arg_section = null)
        {
            if (arg_section == null)
            {
                arg_section = new Section(keyEvent, this);
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
            ResetSectionsInfoInLine(true);
        }

        private void ResetSectionsInfoInLine(bool do_sort)
        {
            if (do_sort)
                XposSort();

            foreach (var list in applicableSectionListinLine.SectionLists)
            {
                list.Clear();
            }

            foreach (var each in serial_and_xpos)
            {
                int serial_num = each.serial_num;
                for (int i = sections[serial_num].Y_pos, line_serial = 0; i < applicableSectionListinLine.SectionLists.Count; i++)
                {
                    applicableSectionListinLine.SectionLists[i].Add(new SectionInfoInLine(serial_num, line_serial));
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

    internal class Section
    {
        public int serial_number { get; set; }
        public Sheet parent_sheet;
        public int X_pos { get; set; }
        public int X_span { get; set; }
        public int Y_pos { get; set; }
        public int Y_span { get; set; }

        public bool Is_changed_in_page { get; set; }

        public int Page_starting_y_pos { get; set; }
        public int Page_starting_x_pos { get; set; }

        public int Total_writed_line_count { get; set; } = 0;
        public int Whole_Length_in_English { get; set; } = 0;
        public List<int> Length_in_English_List { get; set; } = new List<int>();
        private int section_layer_serial_num = 0;

        public List<SectionLayer> layers { get; set; }
        private IKeyEvent keyEvent;

        public Section(IKeyEvent keyEvent, Sheet parent_sheet)
        {
            layers = new List<SectionLayer>();
            this.keyEvent = keyEvent;
            this.parent_sheet = parent_sheet;
        }

        public void SetXYPosAndSpan(int? x_pos = null, int? y_pos = null, int? x_span = null, int? y_span = null)
        {
            X_pos = x_pos ?? X_pos;
            Y_pos = y_pos ?? Y_pos;
            X_span = x_span ?? X_span;
            Y_span = y_span ?? Y_span;
        }

        public int GetPageFinishingY()
        {
            return Page_starting_y_pos + Y_span - 1;
        }

        public int GetPageFinishingX()
        {
            return Page_starting_x_pos + X_span - 1;
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
            while (keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Up)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Down)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Left)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Right)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Enter))
            {
                Thread.Sleep(30);
            }
        }
        public void UpPage(bool for_key)
        {
            Console.WriteLine("up");
            int previous_page_y = Page_starting_y_pos;
            if (Page_starting_y_pos != 0)
            {
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
                if (Is_changed_in_page)
                {
                    parent_sheet.parents_render_class.RenderingOnConsole();
                }
                Wait();
            }
        }
        public void DownPage(bool for_key)
        {
            Console.WriteLine("down");
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
                if (Is_changed_in_page)
                {
                    parent_sheet.parents_render_class.RenderingOnConsole();
                }
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
            if (Page_starting_x_pos > Whole_Length_in_English)
            {
                Page_starting_x_pos = Whole_Length_in_English - X_span;
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

        Singular = 1,
        PluralStart = 2,
        //PluralMiddle = 3,
        PluralEnd = 4,
    }

    public static class TUIColorString
    {
        public const string BlackLetter = "\u001b[30m";
        public const string RedLetter = "\u001b[31m";
        public const string GreenLetter = "\u001b[32m";
        public const string WriteLetter = "";
        public const string Reset = "\u001b[0m";
        public const string WriteBack = "\u001b[47m";
        public const string None = "";
    }

    internal enum TUIColorEnum
    {
        None,

        Reset,
        RedLetter = 31,
        GreenLetter = 32,
        WriteLetter = 37,
        BlackLetter = 30,

        WriteBack = 47
    }

    internal class SectionCharInfo
    {
        public TUIColorEnum color_arg1 = TUIColorEnum.None;
        public TUIColorEnum color_arg2 = TUIColorEnum.None;
        public CharType type = CharType.Empty;
        public char charactor = '\0';
    }

    internal class SectionTextInfoInLine
    {
        public List<SectionCharInfo> char_info_list = new();
        public int length_in_English = 0;
        public bool Is_changed = false;
    }

    internal class ColorParser
    {
        private class StrStreamer
        {
            private string? str = null;
            public int str_i { get; private set; }
            public char? current_char { get; private set; }
            public StrStreamer()
            {

            }

            public void SetElement(string arg_str, int arg_str_i)
            {
                str = arg_str;
                str_i = arg_str_i;
            }

            public char? Consume()
            {
                //Console.WriteLine("consume");
                if (str_i != str.Length)
                {
                    //Console.WriteLine("!!");
                    str_i++;
                    //Console.WriteLine("\'" + str[str_i - 1] + "\'");
                    return str[str_i - 1];
                }
                else
                {
                    //Console.WriteLine("??");
                    return null;
                }
            }
        }

        private StrStreamer streamer = new StrStreamer();
        public ColorParser()
        {

        }

        internal TUIColorEnum? Parse(string str, int str_i)
        {
            streamer.SetElement(str, str_i);
            streamer.Consume(); //\u001b.

            char? c = streamer.Consume();
            //Console.WriteLine("1c : " + c);
            if (c == '[')
            {
                //Console.WriteLine("[");
                return ParseTheFirstDigit();
            }
            else
            {
                return null;
            }
        }

        private TUIColorEnum? ParseTheFirstDigit()
        {
            char? c = streamer.Consume();
            //Console.WriteLine("2c : " + c);
            if (c == '0')
            {
                //Console.WriteLine("0");
                return ParseM(TUIColorEnum.Reset);
            }
            else if (c == '3')
            {
                //Console.WriteLine("3");
                return ParseThreeToTheSecondDigit();
            }
            else if (c == '4')
            {
                //Console.WriteLine("4");
                return ParseFourToTheSecondDigit();
            }
            else
            {
                return null;
            }
        }

        private TUIColorEnum? ParseThreeToTheSecondDigit()
        {
            char? c = streamer.Consume();
            //Console.WriteLine("31c : " + c);
            if (c == '0')
            {
                //Console.WriteLine("0");
                return ParseM(TUIColorEnum.BlackLetter);
            }
            else if (c == '1')
            {
                //Console.WriteLine("1");
                return ParseM(TUIColorEnum.RedLetter);
            }
            else if (c == '2')
            {
                //Console.WriteLine("2");
                return ParseM(TUIColorEnum.GreenLetter);
            }
            else
            {
                return null;
            }
        }

        private TUIColorEnum? ParseFourToTheSecondDigit()
        {
            char? c = streamer.Consume();
            //Console.WriteLine("32c : " + c);
            if (c == '7')
            {
                //Console.WriteLine("7");
                return ParseM(TUIColorEnum.WriteBack);
            }
            else
            {
                return null;
            }
        }

        private TUIColorEnum? ParseM(TUIColorEnum retun_enum)
        {
            if (streamer.Consume() == 'm')
            {
                //Console.WriteLine("m");
                return retun_enum;
            }
            else
            {
                return null;
            }
        }

        public int StrI()
        {
            return streamer.str_i;
        }
    }

    internal class SectionLayer : IRenderingElementRelatedInConsoleSize
    {
        public int serial_number { get; set; }
        public List<SectionTextInfoInLine> texts_info { get; private set; }
        public int Total_writed_line_count { get; set; } = 0;
        private int previous_current_y_when_write = 0;
        private int current_x = 0;
        private int current_y = 0;
        private Section parent_section;
        private bool is_fix_current_x = false;
        private int fix_x_into = 0;
        private ColorInputHelper color_input_helper = new();
        private ColorParser colorParser = new();
        public bool IsClearedCurrently { get; set; } = true;

        public SectionLayer(IKeyEvent keyEvent, Section parent_section)
        {
            texts_info = new List<SectionTextInfoInLine>();
            this.parent_section = parent_section;
        }

        public void ReSetConsoleSize()
        {

        }

        public int GetLenOfTextsList()
        {
            return texts_info.Count;
        }

        //Clearというのは、内部的にはlengthを0にしているだけであって、文字列データは書き換えない.
        //これによりWriteの時に、以前と同じデータのときに、一文字単位で書き換えをスキップすることができる.
        //
        //但し、急にSetCursolでxを10とかにした場合は、10までにあるデータをemptyで埋めることによって、あたかも内部的に
        //文字列データがClearになっているように見せる.(SetEmptyOnPreviousWritedCharactor()により実装)
        public void Clear()
        {
            current_x = 0;
            current_y = 0;
            is_fix_current_x = false;
            Total_writed_line_count = 0;
            foreach (var info_in_line in texts_info)
            {
                info_in_line.length_in_English = 0;
                info_in_line.Is_changed = false;
            }
            color_input_helper.ResetWhenClear();
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

        public void UnFixX()
        {
            is_fix_current_x = false;
        }

        private enum SimpleCharType
        {
            None,
            English,
            Japanese,
            ESC,
            NewLine
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
            else if (c == '\u001b')
            {
                return SimpleCharType.ESC;
            }
            else if (c == '\n')
            {
                return SimpleCharType.NewLine;
            }

            return SimpleCharType.None;
        }

        private void MakeUpYListsBlanckUntil(int y)
        {
            while (!(texts_info.Count > y))
            {
                texts_info.Add(new SectionTextInfoInLine());
            }

            while (!(parent_section.Length_in_English_List.Count > y))
            {
                parent_section.Length_in_English_List.Add(0);
            }
        }

        private void MakeUpXListsBlanckUntil(int x)
        {
            while (!(texts_info[current_y].char_info_list.Count > x))
            {
                texts_info[current_y].char_info_list.Add(new SectionCharInfo());
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

        private void SetInfo(int x, CharType type, char c, TUIColorEnum? color1 = null, TUIColorEnum? color2 = null)
        {
            texts_info[current_y].char_info_list[x].type = type;
            texts_info[current_y].char_info_list[x].charactor = c;
            if (color1 != null)
            {
                //Console.WriteLine("CCCCCCCCCCOOOOOOOOOOLLLLLLLOOOOOOOORRRRRRR");
                //Console.WriteLine((TUIColorEnum)color1);
                texts_info[current_y].char_info_list[x].color_arg1 = (TUIColorEnum)color1;
            }
            if (color2 != null)
            {
                //Console.WriteLine("CCCCCCCCCCOOOOOOOOOOLLLLLLLOOOOOOOORRRRRRR");
                //Console.WriteLine((TUIColorEnum)color2);
                texts_info[current_y].char_info_list[x].color_arg2 = (TUIColorEnum)color2;
            }
        }

        private void SetInfoInEmptyAndProceedX(ref int x)
        {
            SetInfo(x, CharType.Empty, ' ', TUIColorEnum.None, TUIColorEnum.None);
            x++;
        }

        private void SetInfoInEmpty()
        {
            SetInfo(current_x, CharType.Empty, ' ', TUIColorEnum.None, TUIColorEnum.None);
        }

        private void SetInfoInEnglishAndProceedX(ref int x, char c, TUIColorEnum? color1 = null, TUIColorEnum? color2 = null)
        {
            SetInfo(x, CharType.Singular, c, color1, color2);
            x++;
        }
        private void SetInfoInJapaneseAndProceedX(ref int x, char c, TUIColorEnum color1, TUIColorEnum color2)
        {
            SetInfo(x, CharType.PluralStart, c, color1, color2);
            SetInfo(x + 1, CharType.PluralEnd, c, color1, color2);
            x += 2;
        }

        private void SetTotalLineCountAndLength(bool is_write_empty)
        {
            //set total writed line cout in this section layer 
            if (current_y > Total_writed_line_count - 1)
            {
                Total_writed_line_count = current_y + 1;
            }
            //set total writed line cout in parent section
            if (Total_writed_line_count > parent_section.Total_writed_line_count)
            {
                parent_section.Total_writed_line_count = Total_writed_line_count;
            }

            if (!is_write_empty)
            {
                //set length in section text info
                if (current_x > texts_info[current_y].length_in_English - 1)
                {
                    texts_info[current_y].length_in_English = current_x;
                }
                //set length in parent_section
                if (texts_info[current_y].length_in_English > parent_section.Length_in_English_List[current_y])
                {
                    parent_section.Length_in_English_List[current_y] = texts_info[current_y].length_in_English;
                }
                if (parent_section.Length_in_English_List[current_y] > parent_section.Whole_Length_in_English)
                {
                    parent_section.Whole_Length_in_English = parent_section.Length_in_English_List[current_y];
                }
            }

            //by comparing max length
        }

        private void SetCurrentXandYForWriteLineFn()
        {
            current_y++;
            if (is_fix_current_x)
            {
                current_x = 0;
            }
            else
            {
                current_x = fix_x_into;
            }
        }

        private void MakeUpGapBetweenCurrentXandLengthByEnpty()
        {
            int x = texts_info[current_y].length_in_English;
            while (x < current_x)
            {
                //Console.WriteLine("make up by enpty :: x : " + x + "current_x" + current_x);
                SetInfoInEmpty();
                x++;
            }
        }

        private TUIColorEnum? ESCProcessAndReturnColor(string str, ref int str_i)
        {
            int i_back_up = str_i + 1;
            TUIColorEnum? result = colorParser.Parse(str, str_i);
            if (result == null)
            {
                str_i = i_back_up;
            }
            else
            {
                str_i = colorParser.StrI();
                str_i--;//今一個次のiで、forの次のサイクルで++またされるから、一個ひいとく
            }
            return result;
        }

        public SectionLayer WriteEmpty(int length)
        {
            MakeUpYListsBlanckUntil(current_y);
            MakeUpXListsBlanckUntil(current_x);

            MakeUpGapBetweenCurrentXandLengthByEnpty();

            color_input_helper.SetPreviousColor(TUIColorEnum.None, TUIColorEnum.None);

            parent_section.Is_changed_in_page = IsChangedInPage(length);

            bool is_changed_in_line = false;
            for (int str_i = 0; str_i < length; str_i++)
            {
                SectionCharInfo info = texts_info[current_y].char_info_list[current_x];
                if (info.type == CharType.Empty)
                {
                    current_x++;
                    continue;
                }
                else if (info.type == CharType.PluralEnd && str_i == 0)
                {
                    current_x--;
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', color_input_helper.previous_color1, color_input_helper.previous_color2);
                }

                is_changed_in_line = true;

                SetInfoInEmptyAndProceedX(ref current_x);

                if (str_i == length - 1 && texts_info[current_y].char_info_list[current_x].type == CharType.PluralEnd)
                {
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', color_input_helper.previous_color1, color_input_helper.previous_color2);
                }
            }

            if (is_changed_in_line)
            {
                texts_info[current_y].Is_changed = is_changed_in_line;
            }
            SetTotalLineCountAndLength(true);
            return this;
        }

        public SectionLayer WriteLineEmpty(int length)
        {
            WriteEmpty(length);
            SetCurrentXandYForWriteLineFn();
            return this;
        }

        public SectionLayer Write(string str)
        {
            MakeUpYListsBlanckUntil(current_y);
            MakeUpXListsBlanckUntil(current_x);

            MakeUpGapBetweenCurrentXandLengthByEnpty();

            //color_input_helper.SetPreviousColor(current_x == 0 ? TUIColorEnum.None : texts_info[current_y].char_info_list[current_x - 1].color_arg1,
            //    current_x == 0 ? TUIColorEnum.None : texts_info[current_y].char_info_list[current_x - 1].color_arg2);

            parent_section.Is_changed_in_page = IsChangedInPage(str.Length);

            bool is_changed_in_line = false;

            //Console.WriteLine("$$$$$$$$$$$");
            //Console.WriteLine(str);
            //Console.WriteLine("$$$$$$$$$$$");

            for (int str_i = 0; str_i < str.Length; str_i++)
            {
                //Console.WriteLine("####################");
                //Console.WriteLine(str_i);
                //Console.WriteLine("current_x : " + current_x);

                //日本語入力の可能性を考慮してcurrent + 1を分までメモリを確保しておいて、index was out of rangeを防ぐ.
                MakeUpXListsBlanckUntil(current_x + 1);

                SectionCharInfo info = texts_info[current_y].char_info_list[current_x];

                //もし、処理対象のcharがそこに前書かれていた文字列なら、currentだけ進めて次に進む.
                //Console.WriteLine("\'" + info.charactor + "\' == " + "\'" + str[str_i] + "\'" + " && " + info.type);

                if (info.charactor == str[str_i] && info.type == CharType.PluralStart)
                {
                    //Console.WriteLine("japanese equal");
                    if (info.color_arg1 != color_input_helper.previous_color1 || info.color_arg2 != color_input_helper.previous_color2)
                    {
                        is_changed_in_line = true;
                        info.color_arg1 = color_input_helper.previous_color1;
                        info.color_arg2 = color_input_helper.previous_color2;
                    }
                    current_x += 2;
                    continue;
                }
                else if (info.charactor == str[str_i] && info.type == CharType.Singular)
                {
                    //Console.WriteLine("English equal");
                    if (info.color_arg1 != color_input_helper.previous_color1 || info.color_arg2 != color_input_helper.previous_color2)
                    {
                        is_changed_in_line = true;
                        info.color_arg1 = color_input_helper.previous_color1;
                        info.color_arg2 = color_input_helper.previous_color2;
                    }
                    current_x++;
                    continue;
                }
                //日本語の終わりが、初めの時のcurrent_xの場合、日本語の初めの部分を" "で埋める.
                else if (info.type == CharType.PluralEnd && str_i == 0)
                {
                    current_x--;
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', TUIColorEnum.None, TUIColorEnum.None);
                }

                //Console.WriteLine("is_changed_in_line");
                is_changed_in_line = true;

                switch (JudgeCharType(str[str_i]))
                {
                    case SimpleCharType.English:
                    case SimpleCharType.None:
                        //Console.WriteLine("none/English : \'" + str[str_i] + "\'");
                        SetInfoInEnglishAndProceedX(ref current_x, str[str_i], color_input_helper.previous_color1, color_input_helper.previous_color2);
                        break;

                    case SimpleCharType.Japanese:
                        SetInfoInJapaneseAndProceedX(ref current_x, str[str_i], color_input_helper.previous_color1, color_input_helper.previous_color2);
                        break;

                    case SimpleCharType.ESC:
                        //Console.WriteLine("ESC : \'" + str[str_i] + "\'");

                        TUIColorEnum? color = ESCProcessAndReturnColor(str, ref str_i);
                        color_input_helper.ThisColorWasWrited(color);

                        //Console.WriteLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
                        //Console.WriteLine("end color");
                        //Console.WriteLine(color_input_helper.previous_color1 + ", " + color_input_helper.previous_color2);
                        //Console.WriteLine(current_x);
                        //Console.WriteLine(str_i);
                        //Console.WriteLine(str.Length);
                        //Console.WriteLine(str_i != str.Length ? str[str_i] : "EOL");
                        //Console.WriteLine(color == null ? "null" : color.ToString());
                        //Console.WriteLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");

                        if (str_i >= str.Length - 1)
                        {
                            texts_info[current_y].char_info_list[current_x].color_arg1 = color_input_helper.previous_color1;
                            texts_info[current_y].char_info_list[current_x].color_arg2 = color_input_helper.previous_color2;
                        }
                        break;

                    default:
                        //Console.WriteLine("default : \'" + str[str_i] + "\'");
                        break;
                }

                //日本語の終わりが、最後の時のcurrent_xの場合、日本語の終わりの部分を" "で埋める.
                MakeUpXListsBlanckUntil(current_x);
                if (str_i >= str.Length - 1 && texts_info[current_y].char_info_list[current_x].type == CharType.PluralEnd)
                {
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', color_input_helper.previous_color1, color_input_helper.previous_color1);
                }
            }

            if (is_changed_in_line)
            {
                texts_info[current_y].Is_changed = is_changed_in_line;
            }
            SetTotalLineCountAndLength(false);
            return this;
        }

        public SectionLayer WriteLine(string str)
        {
            Write(str);
            SetCurrentXandYForWriteLineFn();
            return this;
        }

        public SectionLayer BottunXYSet(Bottun btn)
        {
            btn.section_x = current_x;
            btn.section_y = current_y;
            btn.section_serial = parent_section.serial_number;
            btn.sheet_serial = parent_section.parent_sheet.serial_number;
            return this;
        }
    }

    internal class ColorInputHelper
    {
        public TUIColorEnum previous_color1 { get; private set; }
        public TUIColorEnum previous_color2 { get; private set; }
        private bool is_one = true;
        public ColorInputHelper() { }

        internal void ResetWhenClear()
        {
            is_one = true;
            previous_color1 = TUIColorEnum.None;
            previous_color2 = TUIColorEnum.None;
        }

        internal void ThisColorWasWrited(TUIColorEnum? arg_color)
        {
            if (arg_color == null)
            {
                return;
            }

            if (arg_color == TUIColorEnum.Reset)
            {
                //Console.WriteLine("RRRRRRRREEEEEEEEEESSSSSSSSSSEEEEEEEEEETTTTTTTTTT");
                previous_color1 = TUIColorEnum.None;
                previous_color2 = TUIColorEnum.None;
                return;
            }

            if (is_one)
            {
                //Console.WriteLine("is_one!!!!!!!!!!!!");
                previous_color1 = (TUIColorEnum)arg_color;
                is_one = false;
            }
            else
            {
                //Console.WriteLine("is_two!!!!!!!!!!!!");
                previous_color2 = (TUIColorEnum)arg_color;
                is_one = true;
            }
        }

        internal void SetPreviousColor(TUIColorEnum color1, TUIColorEnum color2)
        {
            previous_color1 = color1;
            previous_color2 = color2;
        }
    }

    //
    internal class BottunSheet
    {
        public BottunQ BQ { get; set; }
        private Thread t1;
        private Action WritePage;
        public BottunSheet(Action writePage)
        {

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

    internal class BottunQ
    {
        private CSharpConsoleTUI cSharpConsoleTUI;
        private IKeyEvent keyEvent;
        private List<List<Bottun>> bottun_queue = new();
        private int bottun_x = 0;
        private int bottun_y = 0;
        private int old_x = 0;
        private int old_y = 0;
        private bool rocked { get; set; } = false;
        private bool dont_release { get; set; } = false;

        private bool some_on_rocked = false;

        public bool Is_move_page_when_bottun_xy_is_out_of_page_range { get; set; } = true;

        private int y_length { get; set; } = 0;
        private int x_length { get; set; } = 0;

        public BottunQ(CSharpConsoleTUI cSharpConsoleTUI)
        {
            this.cSharpConsoleTUI = cSharpConsoleTUI;
            keyEvent = cSharpConsoleTUI.keyEvent;
        }

        public void BeLocked()
        {
            rocked = true;
        }

        public void BeUnlocked()
        {
            rocked = false;
        }

        public Bottun GetBottun(int x, int y)
        {
            return bottun_queue[y][x];
        }

        public Bottun GetSelectedBottun()
        {
            return bottun_queue[bottun_y][bottun_x];
        }

        public void AddNewBottun(
            int x = -1,
            int y = -1,
            bool apeal = true,
            string rabel = "bottun",
            Action? turned_on = null,
            Action? turned_off = null,
            Action? turned_selected = null
            )
        {
            if (y < 0) y = bottun_queue.Count;
            if (x < 0) x = bottun_queue[y].Count;
            bottun_queue[y].Insert(x, new Bottun(y, x, rabel, apeal, turned_on, turned_off, turned_selected));
        }

        public void DereteBottun(int x = -1, int y = -1)
        {
            if (y < 0) y = bottun_queue.Count - 1;
            if (x < 0) x = bottun_queue[y].Count - 1;
            bottun_queue[y].RemoveAt(x);
        }

        public void AddNewList(
            int size_of_list,
            int times = 1,
            int? y = null,
            bool apeal = true,
            string rabel = "bottun",
            Action? turned_on = null,
            Action? turned_off = null,
            Action? turned_selected = null
            )
        {
            for (int t = 0; t < times; t++)
            {
                if (y == null)
                {
                    y = bottun_queue.Count;
                }
                if (0 < size_of_list && 0 <= y && y <= bottun_queue.Count)
                {
                    bottun_queue.Insert((int)y, new List<Bottun>());
                    for (int i = 0; i < size_of_list; i++)
                    {
                        bottun_queue[(int)y].Add(new Bottun(bottun_queue.Count - 1, i, rabel, apeal, turned_on, turned_off, turned_selected));
                    }
                }
            }
        }


        public void DereteList(int y = -1)
        {
            if (y < 0)
            {
                y = bottun_queue.Count - 1;
            }
            bottun_queue.RemoveAt(y);
        }

        public void SetCursolTop(int x, int y)
        {
            bottun_queue[y][x].console_top = Console.GetCursorPosition().Top;
        }

        public void SetSomething(
            int y,
            int x,
            bool? apeal = null,
            string? rabel = null,
            Action? turned_on = null,
            Action? turned_off = null,
            Action? turned_selected = null)
        {
            Bottun bottun = bottun_queue[y][x];
            if (apeal != null)
                bottun.apear = (bool)apeal;
            if (rabel != null)
                bottun.rabel = rabel;
            if (turned_on != null)
                bottun.Action_when_turned_on = turned_on;
            if (turned_off != null)
                bottun.Action_when_turned_off = turned_off;
            if (turned_selected != null)
                bottun.Action_when_turned_selected = turned_selected;
        }

        public void SetRabel(int x, int y, string rabel)
        {
            bottun_queue[y][x].rabel = rabel;
        }

        public void SetApear(int x, int y, bool apear)
        {
            bottun_queue[y][x].apear = apear;
        }

        public bool GetOn(int x, int y)
        {
            return bottun_queue[y][x].on;
        }

        public bool GetSelected(int x, int y)
        {
            return bottun_queue[y][x].selsected;
        }

        public string GetRabel(int x, int y)
        {
            return bottun_queue[y][x].Rabel();
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
            Console.WriteLine(keyEvent.GetIsPressedArrayByIndex(KeyToIndex.S));
            while (keyEvent.GetIsPressedArrayByIndex(KeyToIndex.W)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.A)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.S)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.D)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Enter))
            {
                Thread.Sleep(30);
            }
        }

        private void DoActionWhenSelected()
        {
            Console.WriteLine("do_action");
            if (bottun_queue[bottun_y][bottun_x].Action_when_turned_selected != null)
                bottun_queue[bottun_y][bottun_x].Action_when_turned_selected();
            else
                Console.WriteLine("ohh my god");
        }

        private void MovePageConsideringBottunXY()
        {
            if (Is_move_page_when_bottun_xy_is_out_of_page_range)
            {
                int section_x = bottun_queue[bottun_y][bottun_x].section_x;
                int last_section_x = section_x + bottun_queue[bottun_y][bottun_x].rabel.Length - 1;
                int section_y = bottun_queue[bottun_y][bottun_x].section_y;
                Section section = cSharpConsoleTUI.renderingForConsole
                    .GetSheet(bottun_queue[bottun_y][bottun_x].sheet_serial)
                    .GetSection(bottun_queue[bottun_y][bottun_x].section_serial);
                while (
                    (section_x < section.Page_starting_x_pos) ||
                    (last_section_x >= section.GetPageFinishingX()) ||
                    (section_y < section.Page_starting_y_pos) ||
                    (section_y > section.GetPageFinishingY())
                    )
                {
                    //Console.WriteLine("============================================================");
                    //Console.WriteLine("section_x : " + section_x + " <  page_start_x : " + section.Page_starting_x_pos);
                    //Console.WriteLine("section_x : " + section_x + " >= page_finish_x : " + section.GetPageFinishingX());
                    //Console.WriteLine("section_y : " + section_y + " <  page_start_y : " + section.Page_starting_y_pos);
                    //Console.WriteLine("section_y : " + section_y + " >  page_finish_y : " + section.GetPageFinishingY());

                    if (section_x < section.Page_starting_x_pos)
                    {
                        section.LeftSlidePage(for_key: false);
                    }
                    if (last_section_x >= section.GetPageFinishingX())
                    {
                        section.RightSlidePage(for_key: false);
                    }
                    if (section_y < section.Page_starting_y_pos)
                    {
                        section.UpPage(for_key: false);
                    }
                    if (section_y > section.GetPageFinishingY())
                    {
                        section.DownPage(for_key: false);
                    }
                }
            }
        }

        public void UpProcess()
        {
            Console.WriteLine(bottun_x + ", " + bottun_y);
            if (rocked)
                return;

            if (bottun_y <= 0 || some_on_rocked)
                return;

            XYGetOld();
            if (bottun_x > bottun_queue[bottun_y - 1].Count - 1)
            {
                bottun_x = bottun_queue[bottun_y - 1].Count - 1;
            }
            bottun_y--;
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
        }

        public void DownProcess()
        {
            Console.WriteLine(bottun_x + ", " + bottun_y);
            if (rocked)
                return;

            if (bottun_y >= bottun_queue.Count - 1 || some_on_rocked)
                return;

            XYGetOld();
            if (bottun_x > bottun_queue[bottun_y + 1].Count - 1)
            {
                bottun_x = bottun_queue[bottun_y + 1].Count - 1;
            }
            bottun_y++;
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
        }

        public void LeftSlideProcess()
        {
            Console.WriteLine(bottun_x + ", " + bottun_y);
            if (rocked)
                return;

            if (bottun_x <= 0 || some_on_rocked)
                return;

            XYGetOld();
            bottun_x--;
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
        }

        public void RightSlideProcess()
        {
            Console.WriteLine(bottun_x + ", " + bottun_y);
            if (rocked)
                return;

            if (bottun_y > bottun_queue.Count - 1 || bottun_x + 1 > bottun_queue[bottun_y].Count - 1 || some_on_rocked)
                return;

            XYGetOld();
            bottun_x++;
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
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

        public async void FastUpSelected()
        {
            UpProcess();
            await Task.Delay(500);
        }

        public async void FastDownSelected()
        {
            DownProcess();
            await Task.Delay(500);
        }

        public async void FastLeftSlideSelected()
        {
            LeftSlideProcess();
            await Task.Delay(500);
        }

        public async void FastRightSlideSelected()
        {
            RightSlideProcess();
            await Task.Delay(500);
        }

        public void TurnOnToKeyEvent()
        {
            if (!some_on_rocked)
            {
                try
                {
                    bottun_queue[bottun_y][bottun_x].on = true;
                    DoActionWhenSelected();
                    bottun_queue[bottun_y][bottun_x].FunctionExcute();
                    some_on_rocked = true;
                }
                catch (IndexOutOfRangeException ex)
                {
                    DoActionWhenSelected();
                    Console.WriteLine(ex.Message);
                }
                Wait();
            }
        }

        public void TurnOn(int y, int x)
        {
            if (!some_on_rocked)
            {
                bottun_queue[y][x].on = true;
                DoActionWhenSelected();
                bottun_queue[y][x].FunctionExcute();
                some_on_rocked = true;
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
                DoActionWhenSelected();
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
            DoActionWhenSelected();
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
        public int sheet_serial { get; set; }
        public int section_serial { get; set; }
        public int section_x { get; set; }
        public int section_y { get; set; }
        public string rabel = "";
        public bool on { get; set; } = false;
        public bool selsected { get; set; } = false;
        public bool apear { get; set; } = true;

        public Action? Action_when_turned_on { get; set; }
        public Action? Action_when_turned_off { get; set; }
        public Action? Action_when_turned_selected { get; set; }
        public Bottun(int y, int x, string rabel = "bottun", bool apear = true, Action? on_ = null, Action? off_ = null, Action? selected_ = null)
        {
            this.x = x;
            this.y = y;
            this.rabel = rabel;
            this.apear = apear;
            this.Action_when_turned_on = on_;
            this.Action_when_turned_off = off_;
            this.Action_when_turned_selected = selected_;
        }

        public void FunctionExcute()
        {
            if (Action_when_turned_on != null)
            {
                Action_when_turned_on();
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
                    return TUIColorString.BlackLetter + TUIColorString.WriteBack + "<< " + rabel + " >>" + TUIColorString.Reset;
                }
                else
                {
                    return "<< " + rabel + " >>";
                }
            }
            else
            {
                return "";
            }
        }
    }
}

