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

namespace src
{
    

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
            if (actions[(int)keyToIndex] == null)
                return;
            foreach (Action? action in actions[(int)keyToIndex])
            {
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
                    if (keyEvent.GetIsPressedArrayByIndex((KeyToIndex)i))
                    {
                        ExecuteActions((KeyToIndex)i);
                    }
                }
                //cpu負荷軽減.
                await Task.Delay(10);
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
        TUIColorEnum _color1 = TUIColorEnum.None;
        TUIColorEnum _color2 = TUIColorEnum.None;
        StringBuilder stringBuilder = new StringBuilder();
        private Dictionary<TUIColorEnum, string> keyValuePairs = new Dictionary<TUIColorEnum, string>
        {   { TUIColorEnum.Reset, TUIColorString.Reset},
            {TUIColorEnum.BlackLetter, TUIColorString.BlackLetter },
            {TUIColorEnum.WriteLetter, TUIColorString.WriteLetter },
            {TUIColorEnum.GreenLetter, TUIColorString.GreenLetter },
            {TUIColorEnum.RedLetter, TUIColorString.RedLetter },
            {TUIColorEnum.WriteBack, TUIColorString.WriteBack }
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
            if (color_arg1 != TUIColorEnum.None &&
                (color_arg1 != _color1 && color_arg1 != _color2) &&
                keyValuePairs.TryGetValue(color_arg1, out string? color1_str))
            {
                stringBuilder.Append(color1_str);
            }
            _color1 = color_arg1;
            if (color_arg2 != TUIColorEnum.None &&
                (color_arg2 != _color1 && color_arg2 != _color2) &&
                keyValuePairs.TryGetValue(color_arg2, out string? color2_str))
            {
                stringBuilder.Append(color2_str);
            }
            _color2 = color_arg2;
            return stringBuilder;
        }

        public StringBuilder ReturnColorStrToWriteOneSpace()
        {
            //もし前に何かあったら、もしくはResetされていなかったら
            if (!((_color1 == TUIColorEnum.None && _color2 == TUIColorEnum.None) ||
                (
                 (_color1 == TUIColorEnum.Reset && (_color2 == TUIColorEnum.None || _color2 == TUIColorEnum.Reset)) ||
                 _color2 == TUIColorEnum.Reset))
                )
            {
                stringBuilder.Append(TUIColorString.Reset);
            }
            _color1 = TUIColorEnum.Reset;
            _color2 = TUIColorEnum.None;
            return stringBuilder;
        }
    }

    internal class CharBufferContext
    {
        public int How_many_x_wrote { get; set; }
        public int Console_x { get; set; }
        public int Console_y { get; set; }
        public int Section_x { get; set; }
        public int Section_y { get; set; }
        public bool Is_end_x { get; set; } = false;
        public bool Is_end_y { get; set; } = false;
        public bool Is_end_section = false;

        private ApplicableSectionListInLine? applicableSectionListInLine = null;
        private Sheet? sheetToRender = null;
        private bool did_this_class_neccesary_init = false;
        private int console_width;
        private int console_height;
        private int applicable_sections_index = 0;
        private int how_many_chars_did_write;
        private ColorManegementInCharBufferContext colorManegementInCharBufferContext = new();
        private ResultCharInfo resultCharInfo = new();
        private Section? section = null;

        public CharBufferContext()
        {
            
        }

        public void ReStartFromBeggining(Sheet arg_sheet)
        {
            did_this_class_neccesary_init = true;

            sheetToRender = arg_sheet;
            applicableSectionListInLine = sheetToRender.applicableSectionListinLine;

            Is_end_section = false;
            Is_end_x = false;
            Is_end_y = false;

            console_height = Console.WindowHeight;
            console_width = Console.WindowWidth;
            Console_y = 0;
            Console_x = 0;

            //インデックスアクセスの安全装置
            if (Console_y <= applicableSectionListInLine.SectionLists.Count - 1)
            {
                SectionInfoInLine sectionInfoInLine = applicableSectionListInLine.SectionLists[Console_y][applicable_sections_index];
                section = sheetToRender.GetSection(sectionInfoInLine.section_serial_num);
                Section_y = section.Page_starting_y_pos + sectionInfoInLine.line_serial;
                Section_x = section.Page_starting_x_pos;
            }
        }

        

        public void GoToNextLine()
        {
            Console_y++;
            //もしConsole_yがheight越すなら、Yのエンド
            if (Console_y > console_height - 1)
            {
                Is_end_y = true;
            }
            InitX();
            InitSection();
        }

        public void ResetHowManyXWrote()
        {

        }

        private void InitSection()
        {
            //インデックスアクセスの安全装置
            if (Console_y > applicableSectionListInLine.SectionLists.Count - 1)
            {
                return;
            }
            SectionInfoInLine sectionInfoInLine = applicableSectionListInLine.SectionLists[Console_y][applicable_sections_index];
            section = sheetToRender.GetSection(sectionInfoInLine.section_serial_num);
            Section_y = section.Page_starting_y_pos + sectionInfoInLine.line_serial;
            Section_x = section.Page_starting_x_pos;
        }

        private void InitX()
        {
            Console_x = 0;
            applicable_sections_index = 0;
        }

        public ResultCharInfo? ConsumeChar()
        {
            //initできてなかったら.
            if (section == null || !did_this_class_neccesary_init)
            {
                return null;
            }

            //endの時の仕切り直し
            if (Is_end_x)
            {
                InitX();
            }
            if (Is_end_section)
            {
                InitSection();
            }

            SectionLayer layer;
            CharType charType;
            SectionCharInfo sectionCharInfo;

            //後ろからlayerを回す.
            for (int i = 0, layer_index = section.layers.Count - 1; i < section.layers.Count; i++, layer_index--)
            {
                layer = section.GetSectionLayer(layer_index);

                //x, yに文字がなかったらcontinu(section.section_layer.texts_infoのインデックスアクセスの安全装置)
                if (Section_y > layer.Total_writed_line_count - 1 || Section_x > layer.texts_info[Section_y].length_in_English - 1)
                {
                    continue;
                }

                sectionCharInfo = layer.texts_info[Section_y].char_info_list[Section_x];
                charType = sectionCharInfo.type;

                switch (charType)
                {
                    case CharType.Empty:

                        if (layer_index == 0)
                        {
                            if (console_width - 1 < Section_x + 1)
                                goto break_layer_for_stmt;

                            resultCharInfo.stringBuilder.Append(
                                colorManegementInCharBufferContext.ReturnColorStrToWriteOneSpace());

                            resultCharInfo.stringBuilder.Append(' ');

                            Section_x++;
                            Console_x++;
                            how_many_chars_did_write++;
                        }
                        continue;

                    case CharType.Singular:
                        if (console_width - 1 < Section_x + 1)
                            goto break_layer_for_stmt;

                        resultCharInfo.stringBuilder.Append(
                            colorManegementInCharBufferContext.ReturnColorStrToWriteChar(sectionCharInfo.color_arg1, sectionCharInfo.color_arg2)
                        );

                        resultCharInfo.stringBuilder.Append(sectionCharInfo.charactor);

                        Section_x++;
                        Console_x++;
                        how_many_chars_did_write++;

                        goto break_layer_for_stmt;

                    case CharType.PluralStart:
                        if (Section_x + 2 > section.Page_starting_x_pos + section.X_span)
                        {
                            if (console_width - 1 < Section_x + 1)
                                goto break_layer_for_stmt;

                            resultCharInfo.stringBuilder.Append(
                                colorManegementInCharBufferContext.ReturnColorStrToWriteOneSpace());

                            resultCharInfo.stringBuilder.Append(' ');

                            Section_x++;
                            Console_x++;
                            how_many_chars_did_write++;
                        }
                        else
                        {
                            if (console_width - 1 < Section_x + 2)
                                goto break_layer_for_stmt;

                            resultCharInfo.stringBuilder.Append(
                                colorManegementInCharBufferContext.ReturnColorStrToWriteChar(sectionCharInfo.color_arg1, sectionCharInfo.color_arg2)
                            );
                            resultCharInfo.stringBuilder.Append(sectionCharInfo.charactor);

                            Section_x += 2;
                            Console_x += 2;
                            how_many_chars_did_write += 2;
                        }
                        goto break_layer_for_stmt;

                    default:
                        break;

                }
            }
        break_layer_for_stmt:
            ;

            //もしこのセクション描画し終わったなら、このYの次のSectionへ.
            if (Section_y > section.Length_in_English_List[Section_y] - 1)
            {
                applicable_sections_index++;
            }

            //Console_xはすでに一つ先を指す
            //もしwidth越しそうなら、もしくは、もし全セクション描画したなら、Xのエンド
            if (Console_x > console_width + 1 
                || applicable_sections_index > applicableSectionListInLine.SectionLists[Console_y].Count - 1
                )
            {
                Console_y++;
                Is_end_section = true;
                Is_end_x = true;
            }
            //もしConsole_yがheight越すなら、Yのエンド
            if (Console_y > console_height - 1)
            {
                Is_end_y = true;
            }

            return resultCharInfo;
        }
    }

    internal class RenderingClassForConsole
    {
        private List<Sheet> sheet_q;
        private Sheet? sheet_to_render;
        private IKeyEvent keyEvent;
        private List<StringBuilder> text_to_write = new();
        private StringBuilder fainal_sb_to_write = new();
        private int console_y_length;
        private int sheet_serial_num = 0;
        private CharBufferContext charBufferContext;
        private ResultCharInfo? resultCharInfo;
        private Dictionary<TUIColorEnum, string> keyValuePairs = new Dictionary<TUIColorEnum, string>
        {   { TUIColorEnum.Reset, TUIColorString.Reset},
            {TUIColorEnum.BlackLetter, TUIColorString.BlackLetter },
            {TUIColorEnum.WriteLetter, TUIColorString.WriteLetter },
            {TUIColorEnum.GreenLetter, TUIColorString.GreenLetter },
            {TUIColorEnum.RedLetter, TUIColorString.RedLetter },
            {TUIColorEnum.WriteBack, TUIColorString.WriteBack }
        };

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

        public void AddSheet(int times)
        {
            for (int i = 0; i < times; i++)
            {
                Sheet sheet = new Sheet(keyEvent);
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

        public void RenderingOnConsole()
        {
            if (sheet_to_render == null)
            {
                Console.WriteLine("Render");
                return;
            }

            charBufferContext.ReStartFromBeggining(sheet_to_render);
            while (!charBufferContext.Is_end_y)
            {
                if (IsNotChangedInALine(charBufferContext.Console_y))
                {
                    charBufferContext.GoToNextLine();
                    continue;
                }

                while (!charBufferContext.Is_end_x)
                {
                    resultCharInfo = charBufferContext.ConsumeChar();
                    if (resultCharInfo != null)
                    {
                        text_to_write[charBufferContext.Console_y].Append(resultCharInfo.stringBuilder);
                    }
                }
            }

            Console.WriteLine("Render");
            Console.WriteLine(console_y_length);

            //できたtexts_to_writeを一つのStringBuilderにして描画する.
            for (int i = 0; i < console_y_length; i++)
            {
                fainal_sb_to_write.Append(text_to_write[i]);
                
                if (i != console_y_length - 1)
                {
                    fainal_sb_to_write.Append(i);
                    //fainal_sb_to_write.Append("\n");
                }
            }
            Console.SetCursorPosition(0, 0);
            Console.Write(fainal_sb_to_write);
        }

        private bool IsNotChangedInALine(int text_to_write_y)
        {
            if (text_to_write_y > sheet_to_render.applicableSectionListinLine.SectionLists.Count - 1)
            {
                return true;
            }

            //変更されているかされていないか判定.
            //されていたら次のコンソール行へ.
            //このyに入っているsectionを回して、全てのセクションが変更なし、か、一つでも変更有かどうかを調べる.
            bool no_section_is_changed = true;
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
                        no_layer_is_changed = false;
                        break;
                    }
                }

                if (!no_layer_is_changed)
                {
                    no_section_is_changed = false;
                    break;
                }
            }

            return no_section_is_changed;
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
        public ApplicableSectionListInLine applicableSectionListinLine { get; private set; } = new();
        private List<int> section_serial_number_s = new();
        private List<int> section_x_pos_list = new();
        private List<SectionSerialNumberAndXpos> serial_and_xpos = new();
        private IKeyEvent keyEvent;

        public Sheet(IKeyEvent keyEvent)
        {
            sections = new List<Section>();
            this.keyEvent = keyEvent;
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
        public List<int> Length_in_English_List { get; set; }= new List<int>();
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
            while (keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Up) 
                | keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Down) 
                | keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Left) 
                | keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Right) 
                | keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Enter))
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

        Reset = 0,

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

        private void SetInfo(int x, CharType type, char c, TUIColorEnum? color1= null , TUIColorEnum? color2 = null)
        {
            texts_info[current_y].char_info_list[x].type = type;
            texts_info[current_y].char_info_list[x].charactor = c;
            if (color1 != null)
                texts_info[current_y].char_info_list[x].color_arg1 = (TUIColorEnum)color1;
            if (color2 != null)
                texts_info[current_y].char_info_list[x].color_arg2 = (TUIColorEnum)color2;
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
                if (current_y > texts_info[current_y].length_in_English - 1)
                {
                    texts_info[current_y].length_in_English = current_x;
                }
                //set length in parent_section
                if (texts_info[current_y].length_in_English > parent_section.Whole_Length_in_English)
                {
                    parent_section.Whole_Length_in_English = texts_info[current_y].length_in_English;
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

        private void SetEmptyOnPreviousWritedCharactor()
        {
            for (int x = texts_info[current_y].length_in_English - 1; x < current_x + 1; x++)
            {
                SetInfoInEmpty();
            }
        }

        private TUIColorEnum JudgeColorNumber(string str, ref int str_i)
        {
            char first = '\0';
            char second = '\0';
            if (str.Length - 1 != str_i + 1)
            {
                first = str[str_i + 1];
                str_i++;
            }
            if (str.Length - 1 != str_i + 1)
            {
                second = str[str_i + 1];
                str_i++;
            }

            if (first == '0')
            {
                return TUIColorEnum.Reset;
            }
            if (first == '3' && second == '0')
            {
                return TUIColorEnum.BlackLetter;
            }
            if (first == '3' && second == '1')
            {
                return TUIColorEnum.RedLetter;
            }
            if (first == '3' && second == '2')
            {
                return TUIColorEnum.GreenLetter;
            }
            if (first == '4' && second == '7')
            {
                return TUIColorEnum.WriteBack;
            }
            return TUIColorEnum.None;
        }

        private TUIColorEnum ESCProcessAndReturnColor(string str, ref int str_i)
        {
            bool IsNumber(int i)
            {
                switch (str[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return true;

                    default:
                        return false;
                }
            }

            //\u00b1
            if (str[str_i + 1] == '[' && IsNumber(str_i + 2))
            {
                str_i += 2;
                TUIColorEnum color = JudgeColorNumber(str, ref str_i);
                return color;
            }
            return TUIColorEnum.None;
        }

        public SectionLayer WriteEmpty(int length)
        {
            MakeUpYListsBlanckUntil(current_y);
            MakeUpXListsBlanckUntil(current_x);

            SetEmptyOnPreviousWritedCharactor();

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
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', TUIColorEnum.None, TUIColorEnum.None);
                }

                is_changed_in_line = true;

                SetInfoInEmptyAndProceedX(ref current_x);

                if (str_i == length - 1 && texts_info[current_y].char_info_list[current_x].type == CharType.PluralEnd)
                {
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', TUIColorEnum.None, TUIColorEnum.None);
                }
            }

            texts_info[current_y].Is_changed = is_changed_in_line;
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

            SetEmptyOnPreviousWritedCharactor();

            parent_section.Is_changed_in_page = IsChangedInPage(str.Length);

            bool is_changed_in_line = false;
            TUIColorEnum color_arg1 = TUIColorEnum.None;
            TUIColorEnum color_arg2 = TUIColorEnum.None;
            //もし、Resetが来たら、後継のcolorをnoneにする.
            void ResetColorArg()
            {
                if (color_arg2 == TUIColorEnum.Reset || (color_arg1 == TUIColorEnum.Reset && color_arg2 == TUIColorEnum.None))
                {
                    color_arg1 = TUIColorEnum.None;
                    color_arg2 = TUIColorEnum.None;
                }
            }
            void IfLastProcessForEnglishAndJapenese(int str_i)
            {
                if (str_i == str.Length - 1 && texts_info[current_y].char_info_list[current_x].type == CharType.PluralEnd)
                {
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', color_arg1, color_arg2);
                }
            }
            void IfLastProcessForColor()
            {

            }

            for (int str_i = 0; str_i < str.Length; str_i++)
            {
                //日本語入力の可能性を考慮してcurrent + 1を分までメモリを確保しておいて、index was out of rangeを防ぐ.
                MakeUpXListsBlanckUntil(current_x + 1);

                SectionCharInfo info = texts_info[current_y].char_info_list[current_x];

                //もし、処理対象のcharがそこに前書かれていた文字列なら、currentだけ進めて次に進む.
                if (info.charactor == str[str_i] && info.type == CharType.PluralStart)
                {
                    current_x += 2;
                    continue;
                }
                else if (info.charactor == str[str_i] && info.type == CharType.Singular)
                {
                    current_x++;
                    continue;
                }
                //日本語の終わりが、初めの時のcurrent_xの場合、日本語の初めの部分を" "で埋める.
                else if (info.type == CharType.PluralEnd && str_i == 0)
                {
                    current_x--;
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', TUIColorEnum.None, TUIColorEnum.None);
                }

                is_changed_in_line = true;

                switch (JudgeCharType(str[str_i]))
                {
                    case SimpleCharType.English:
                    case SimpleCharType.None:
                        SetInfoInEnglishAndProceedX(ref current_x, str[str_i], color_arg1, color_arg2);
                        IfLastProcessForColor();
                        ResetColorArg();
                        break;

                    case SimpleCharType.Japanese:
                        SetInfoInJapaneseAndProceedX(ref current_x, str[str_i], color_arg1, color_arg2);
                        ResetColorArg();
                        break;

                    case SimpleCharType.ESC:
                        TUIColorEnum color = ESCProcessAndReturnColor(str, ref str_i);
                        if (color_arg1 == TUIColorEnum.None)
                        {
                            color_arg1 = color;
                        }
                        else
                        {
                            color_arg2 = color;
                        }
                        if (str_i == str.Length - 1)
                        {
                            texts_info[current_y].char_info_list[current_x].color_arg1 = color_arg1;
                            texts_info[current_y].char_info_list[current_x].color_arg2 = color_arg2;
                        }
                        break;

                    default:
                        break;
                }

                //日本語の終わりが、最後の時のcurrent_xの場合、日本語の終わりの部分を" "で埋める.
                MakeUpXListsBlanckUntil(current_x);
                if (str_i == str.Length - 1 && texts_info[current_y].char_info_list[current_x].type == CharType.PluralEnd)
                {
                    SetInfoInEnglishAndProceedX(ref current_x, ' ', color_arg1, color_arg2);
                }
            }
            texts_info[current_y].Is_changed = is_changed_in_line;
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
            int y = -1,
            bool apeal = true, 
            string rabel = "bottun", 
            Action? turned_on = null, 
            Action? turned_off = null, 
            Action? turned_selected = null
            )
        {
            for (int t = 0; t < times; t++)
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
                        bottun_queue[y][i].Action_when_turned_on = turned_on;
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

        public async void Wait()
        {
            while (keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Up)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Down)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Left)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Right)
                || keyEvent.GetIsPressedArrayByIndex(KeyToIndex.Enter))
            {
                await Task.Delay(2);
            }
        }

        private void DoActionWhenSelected()
        {
            if (bottun_queue[bottun_y][bottun_x].Action_when_turned_selected != null)
                bottun_queue[bottun_y][bottun_x].Action_when_turned_selected();
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
                    (last_section_x <= section.GetPageFinishingX()) ||
                    (section_y < section.Page_starting_y_pos) ||
                    (section_y > section.GetPageFinishingY())
                    )
                {
                    if (section_x < section.Page_starting_x_pos)
                    {
                        section.RightSlidePage(for_key: false);
                    }
                    if (last_section_x <= section.GetPageFinishingX())
                    {
                        section.LeftSlidePage(for_key: false);
                    }
                    if (section_y < section.Page_starting_y_pos)
                    {
                        section.DownPage(for_key: false);
                    }
                    if (section_y > section.GetPageFinishingY())
                    {
                        section.UpPage(for_key: false);
                    }
                }
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
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
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
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
        }

        public void LeftSlideProcess()
        {
            if (rocked)
                return;

            if (!((0 < bottun_x) && !some_on_rocked))
                return;

            XYGetOld();
            bottun_x--;
            MovePageConsideringBottunXY();
            ChangeSelected();
            DoActionWhenSelected();
        }

        public void RightSlideProcess()
        {
            if (rocked)
                return;

            if (!((bottun_x < bottun_queue[bottun_y].Count - 1) && !some_on_rocked))
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
                    return   TUIColorString.BlackLetter + TUIColorEnum.WriteBack + "< " + rabel + " >" + TUIColorString.Reset;
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
            RenderingClassForConsole renderingForConsole = new(keyEvent);
            
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
            /*
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
            */
            for (int i = 0; i < 10; i++)
            {
                //BS.BQ.AddNewList(2, function: actiona, rabel: to_rabel);
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

