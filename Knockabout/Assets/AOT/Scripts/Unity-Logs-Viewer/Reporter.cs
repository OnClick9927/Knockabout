

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AOT
{
    public class Reporter : MonoBehaviour
    {
        [System.Serializable]
        class Images
        {
            public Texture2D clear;
            public Texture2D collapse;

            public Texture2D search;
            public Texture2D copy;
            public Texture2D close;


            public Texture2D log_icon;
            public Texture2D warning_icon;
            public Texture2D error_icon;

            public Texture2D bar;
            public Texture2D button_active;
            public Texture2D even_log;
            public Texture2D odd_log;
            public Texture2D selected;

        }

        class MultiKeyDictionary<T1, T2, T3> : Dictionary<T1, Dictionary<T2, T3>>
        {
            new public Dictionary<T2, T3> this[T1 key]
            {
                get
                {
                    if (!ContainsKey(key))
                        Add(key, new Dictionary<T2, T3>());

                    Dictionary<T2, T3> returnObj;
                    TryGetValue(key, out returnObj);

                    return returnObj;
                }
            }

            public bool ContainsKey(T1 key1, T2 key2)
            {
                Dictionary<T2, T3> returnObj;
                TryGetValue(key1, out returnObj);
                if (returnObj == null)
                    return false;

                return returnObj.ContainsKey(key2);
            }
        }

        public void LoadImages(string reporterPath)
        {
#if UNITY_EDITOR
            this.images = new Images();
            var fields = this.images.GetType().GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var tx = UnityEditor.AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, $"Images/{field.Name}.png"), typeof(Texture2D));
                field.SetValue(images, tx);
            }
#endif

        }


        List<DateTime> samples = new List<DateTime>();

        public class Log
        {
            public int count = 1;
            public LogType logType;
            public string condition;
            public string stacktrace;
            public int sampleId;

            public float GetMemoryUsage()
            {
                return (float)(sizeof(int) +
                        sizeof(LogType) +
                        condition.Length * sizeof(char) +
                        stacktrace.Length * sizeof(char) +
                        sizeof(int));
            }
        }
        //contains all uncollapsed log
        List<Log> logs = new List<Log>();
        //contains all collapsed logs
        List<Log> collapsedLogs = new List<Log>();

        List<Log> currentLog = new List<Log>();

        //used to check if the new coming logs is already exist or new one
        MultiKeyDictionary<string, string, Log> logsDic = new MultiKeyDictionary<string, string, Log>();
        //to save memory
        Dictionary<string, string> cachedString = new Dictionary<string, string>();
        bool _show = false;
        bool show
        {
            get => _show;
            set
            {
#if UNITY_EDITOR
                return;
#else
                if (value == _show) return;
                var sys = GameObject.FindObjectOfType<EventSystem>(true);
                sys.gameObject.SetActive(!value);
                _show = value;
#endif
            }
        }
        //collapse logs
        bool collapse;

        bool showLog = true;
        //show or hide warnings
        bool showWarning = true;
        //show or hide errors
        bool showError = true;

        //total number of logs
        int numOfLogs = 0;
        //total number of warnings
        int numOfLogsWarning = 0;
        //total number of errors
        int numOfLogsError = 0;
        //total number of collapsed logs
        int numOfCollapsedLogs = 0;
        //total number of collapsed warnings
        int numOfCollapsedLogsWarning = 0;
        //total number of collapsed errors
        int numOfCollapsedLogsError = 0;










        [SerializeField] Images images;
        // gui
        GUIContent clearContent;
        GUIContent collapseContent;


        //GUIContent showFpsContent;
        GUIContent searchContent;
        GUIContent copyContent;
        GUIContent closeContent;


        GUIContent logContent;
        GUIContent warningContent;
        GUIContent errorContent;
        GUIStyle barStyle;
        GUIStyle buttonActiveStyle;

        GUIStyle nonStyle;
        GUIStyle lowerLeftFontStyle;
        GUIStyle backStyle;
        GUIStyle evenLogStyle;
        GUIStyle oddLogStyle;
        GUIStyle logButtonStyle;
        GUIStyle selectedLogStyle;
        GUIStyle selectedLogFontStyle;
        GUIStyle stackLabelStyle;
        GUIStyle scrollerStyle;
        GUIStyle searchStyle;
        GUIStyle sliderBackStyle;
        GUIStyle sliderThumbStyle;
        //GUISkin toolbarScrollerSkin;
        //GUISkin logScrollerSkin;
        //GUISkin graphScrollerSkin;

        public float size = 32;
        public int numOfCircleToShow = 1;
        string filterText = "";



        void Initialize()
        {
            clearContent = clearContent ?? new GUIContent("", images.clear, "Clear logs");
            collapseContent = collapseContent ?? new GUIContent("", images.collapse, "Collapse logs");


            searchContent = searchContent ?? new GUIContent("", images.search, "Search for logs");
            copyContent = copyContent ?? new GUIContent("", images.copy, "Copy log to clipboard");
            closeContent = closeContent ?? new GUIContent("", images.close, "Hide logs");



            //snapshotContent = new GUIContent("",images.cameraImage,"show or hide logs");
            logContent = logContent ?? new GUIContent("", images.log_icon, "show or hide logs");
            warningContent = warningContent ?? new GUIContent("", images.warning_icon, "show or hide warnings");
            errorContent = errorContent ?? new GUIContent("", images.error_icon, "show or hide errors");





            int paddingX = (int)(size * 0.2f);
            int paddingY = (int)(size * 0.2f);
            nonStyle = new GUIStyle();
            nonStyle.clipping = TextClipping.Clip;
            nonStyle.border = new RectOffset(0, 0, 0, 0);
            nonStyle.normal.background = null;
            nonStyle.fontSize = (int)(size / 2);
            nonStyle.alignment = TextAnchor.MiddleCenter;

            lowerLeftFontStyle = new GUIStyle();
            lowerLeftFontStyle.clipping = TextClipping.Clip;
            lowerLeftFontStyle.border = new RectOffset(0, 0, 0, 0);
            lowerLeftFontStyle.normal.background = null;
            lowerLeftFontStyle.fontSize = (int)(size / 2);
            lowerLeftFontStyle.fontStyle = FontStyle.Bold;
            lowerLeftFontStyle.alignment = TextAnchor.LowerLeft;


            barStyle = new GUIStyle();
            barStyle.border = new RectOffset(1, 1, 1, 1);
            barStyle.normal.background = images.bar;
            barStyle.active.background = images.button_active;
            barStyle.alignment = TextAnchor.MiddleCenter;
            barStyle.margin = new RectOffset(1, 1, 1, 1);

            //barStyle.padding = new RectOffset(paddingX,paddingX,paddingY,paddingY); 
            //barStyle.wordWrap = true ;
            barStyle.clipping = TextClipping.Clip;
            barStyle.fontSize = (int)(size / 2);


            buttonActiveStyle = new GUIStyle();
            buttonActiveStyle.border = new RectOffset(1, 1, 1, 1);
            buttonActiveStyle.normal.background = images.button_active;
            buttonActiveStyle.alignment = TextAnchor.MiddleCenter;
            buttonActiveStyle.margin = new RectOffset(1, 1, 1, 1);
            //buttonActiveStyle.padding = new RectOffset(4,4,4,4);
            buttonActiveStyle.fontSize = (int)(size / 2);

            backStyle = new GUIStyle();
            backStyle.normal.background = images.even_log;
            backStyle.clipping = TextClipping.Clip;
            backStyle.fontSize = (int)(size / 2);

            evenLogStyle = new GUIStyle();
            evenLogStyle.normal.background = images.even_log;
            evenLogStyle.fixedHeight = size;
            evenLogStyle.clipping = TextClipping.Clip;
            evenLogStyle.alignment = TextAnchor.UpperLeft;
            evenLogStyle.imagePosition = ImagePosition.ImageLeft;
            evenLogStyle.fontSize = (int)(size / 2);
            //evenLogStyle.wordWrap = true;

            oddLogStyle = new GUIStyle();
            oddLogStyle.normal.background = images.odd_log;
            oddLogStyle.fixedHeight = size;
            oddLogStyle.clipping = TextClipping.Clip;
            oddLogStyle.alignment = TextAnchor.UpperLeft;
            oddLogStyle.imagePosition = ImagePosition.ImageLeft;
            oddLogStyle.fontSize = (int)(size / 2);
            //oddLogStyle.wordWrap = true ;

            logButtonStyle = new GUIStyle();
            //logButtonStyle.wordWrap = true;
            logButtonStyle.fixedHeight = size;
            logButtonStyle.clipping = TextClipping.Clip;
            logButtonStyle.alignment = TextAnchor.UpperLeft;
            //logButtonStyle.imagePosition = ImagePosition.ImageLeft ;
            //logButtonStyle.wordWrap = true;
            logButtonStyle.fontSize = (int)(size / 2);
            logButtonStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            selectedLogStyle = new GUIStyle();
            selectedLogStyle.normal.background = images.selected;
            selectedLogStyle.fixedHeight = size;
            selectedLogStyle.clipping = TextClipping.Clip;
            selectedLogStyle.alignment = TextAnchor.UpperLeft;
            selectedLogStyle.normal.textColor = Color.white;
            //selectedLogStyle.wordWrap = true;
            selectedLogStyle.fontSize = (int)(size / 2);

            selectedLogFontStyle = new GUIStyle();
            selectedLogFontStyle.normal.background = images.selected;
            selectedLogFontStyle.fixedHeight = size;
            selectedLogFontStyle.clipping = TextClipping.Clip;
            selectedLogFontStyle.alignment = TextAnchor.UpperLeft;
            selectedLogFontStyle.normal.textColor = Color.white;
            //selectedLogStyle.wordWrap = true;
            selectedLogFontStyle.fontSize = (int)(size / 2);
            selectedLogFontStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            stackLabelStyle = new GUIStyle();
            stackLabelStyle.wordWrap = true;
            stackLabelStyle.fontSize = (int)(size / 2);
            stackLabelStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            scrollerStyle = new GUIStyle();
            scrollerStyle.normal.background = images.bar;

            searchStyle = new GUIStyle();
            searchStyle.normal.background = images.even_log;
            searchStyle.alignment = TextAnchor.MiddleLeft;
            searchStyle.fontSize = (int)(size / 2) + 5;
            //searchStyle.clipping = TextClipping.Clip;
            //searchStyle.alignment = TextAnchor.LowerCenter;
            //searchStyle.fontSize = (int)(size.y / 2);
            searchStyle.wordWrap = false;
            searchStyle.richText = true;

            sliderBackStyle = new GUIStyle();
            sliderBackStyle.normal.background = images.bar;
            sliderBackStyle.fixedHeight = size;
            sliderBackStyle.border = new RectOffset(1, 1, 1, 1);

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = images.selected;
            sliderThumbStyle.fixedWidth = size;

            //GUISkin skin = images.reporterScrollerSkin;

            //toolbarScrollerSkin = (GUISkin)GameObject.Instantiate(skin);
            //toolbarScrollerSkin.verticalScrollbar.fixedWidth = 0f;
            //toolbarScrollerSkin.horizontalScrollbar.fixedHeight = 0f;
            //toolbarScrollerSkin.verticalScrollbarThumb.fixedWidth = 0f;
            //toolbarScrollerSkin.horizontalScrollbarThumb.fixedHeight = 0f;

            //logScrollerSkin = (GUISkin)GameObject.Instantiate(skin);
            //logScrollerSkin.verticalScrollbar.fixedWidth = size * 2f;
            //logScrollerSkin.horizontalScrollbar.fixedHeight = 0f;
            //logScrollerSkin.verticalScrollbarThumb.fixedWidth = size * 2f;
            //logScrollerSkin.horizontalScrollbarThumb.fixedHeight = 0f;

            //graphScrollerSkin = (GUISkin)GameObject.Instantiate(skin);
            //graphScrollerSkin.verticalScrollbar.fixedWidth = 0f;
            //graphScrollerSkin.horizontalScrollbar.fixedHeight = size * 2f;
            //graphScrollerSkin.verticalScrollbarThumb.fixedWidth = 0f;
            //graphScrollerSkin.horizontalScrollbarThumb.fixedHeight = size * 2f;



        }
        void Awake()
        {

            void CaptureLogThread(string condition, string stacktrace, LogType type)
            {
                Log log = new Log() { condition = condition, stacktrace = stacktrace, logType = (LogType)type };
                lock (threadedLogs)
                {
                    threadedLogs.Add(log);
                }
            }
            DontDestroyOnLoad(gameObject);
            Initialize();

            //Application.logMessageReceived += CaptureLog ;
            Application.logMessageReceivedThreaded += CaptureLogThread;
        }












        //clear all logs
        void clear()
        {
            logs.Clear();
            collapsedLogs.Clear();
            currentLog.Clear();
            logsDic.Clear();
            //selectedIndex = -1;
            selectedLog = null;
            numOfLogs = 0;
            numOfLogsWarning = 0;
            numOfLogsError = 0;
            numOfCollapsedLogs = 0;
            numOfCollapsedLogsWarning = 0;
            numOfCollapsedLogsError = 0;
            //logsMemUsage = 0;
            //graphMemUsage = 0;
            samples.Clear();
            System.GC.Collect();
            selectedLog = null;
        }

        Rect screenRect = Rect.zero;
        Rect toolBarRect = Rect.zero;
        Rect logsRect = Rect.zero;
        Rect stackRect = Rect.zero;

        Rect buttomRect = Rect.zero;
        //Vector2 stackRectTopLeft;

        Vector2 scrollPosition;
        Vector2 scrollPosition2;

        //int 	selectedIndex = -1;
        Log selectedLog;

        float oldDrag;
        float oldDrag2;
        int startIndex;

        //calculate what is the currentLog : collapsed or not , hide or view warnings ......
        void calculateCurrentLog()
        {
            bool filter = !string.IsNullOrEmpty(filterText);
            string _filterText = "";
            if (filter)
                _filterText = filterText.ToLower();
            currentLog.Clear();
            if (collapse)
            {
                for (int i = 0; i < collapsedLogs.Count; i++)
                {
                    Log log = collapsedLogs[i];
                    if (log.logType == LogType.Log && !showLog)
                        continue;
                    if (log.logType == LogType.Warning && !showWarning)
                        continue;
                    if (log.logType == LogType.Error && !showError)
                        continue;
                    if (log.logType == LogType.Assert && !showError)
                        continue;
                    if (log.logType == LogType.Exception && !showError)
                        continue;

                    if (filter)
                    {
                        if (log.condition.ToLower().Contains(_filterText))
                            currentLog.Add(log);
                    }
                    else
                    {
                        currentLog.Add(log);
                    }
                }
            }
            else
            {
                for (int i = 0; i < logs.Count; i++)
                {
                    Log log = logs[i];
                    if (log.logType == LogType.Log && !showLog)
                        continue;
                    if (log.logType == LogType.Warning && !showWarning)
                        continue;
                    if (log.logType == LogType.Error && !showError)
                        continue;
                    if (log.logType == LogType.Assert && !showError)
                        continue;
                    if (log.logType == LogType.Exception && !showError)
                        continue;

                    if (filter)
                    {
                        if (log.condition.ToLower().Contains(_filterText))
                            currentLog.Add(log);
                    }
                    else
                    {
                        currentLog.Add(log);
                    }
                }
            }

            if (selectedLog != null)
            {
                int newSelectedIndex = currentLog.IndexOf(selectedLog);
                if (newSelectedIndex == -1)
                {
                    Log collapsedSelected = logsDic[selectedLog.condition][selectedLog.stacktrace];
                    newSelectedIndex = currentLog.IndexOf(collapsedSelected);
                    if (newSelectedIndex != -1)
                        scrollPosition.y = newSelectedIndex * size;
                }
                else
                {
                    scrollPosition.y = newSelectedIndex * size;
                }
            }
        }







        Vector2 scroll_toobar;
        void drawToolBar()
        {
            toolBarRect.x = 0f;
            toolBarRect.y = 0f;
            toolBarRect.width = Screen.width;
            toolBarRect.height = size * 2f + 5;
            //GUI.skin = toolbarScrollerSkin;

            GUILayout.BeginArea(toolBarRect);
            scroll_toobar = GUILayout.BeginScrollView(scroll_toobar);

            GUILayout.BeginHorizontal(barStyle);
            {
                if (GUILayout.Button(clearContent, barStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    clear();
                }
                if (GUILayout.Button(collapseContent, (collapse) ? buttonActiveStyle : barStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    collapse = !collapse;
                    calculateCurrentLog();
                }
                if (GUILayout.Button(copyContent, barStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    if (selectedLog == null)
                        GUIUtility.systemCopyBuffer = "No log selected";
                    else
                        GUIUtility.systemCopyBuffer = selectedLog.condition + System.Environment.NewLine + System.Environment.NewLine + selectedLog.stacktrace;
                }


                GUILayout.FlexibleSpace();


                if (collapse)
                {
                    logContent.text = numOfCollapsedLogs.ToString();
                    warningContent.text = numOfCollapsedLogsWarning.ToString();
                    errorContent.text = numOfCollapsedLogsError.ToString();
                }

                else
                {
                    logContent.text = numOfLogs.ToString();
                    warningContent.text = numOfLogsWarning.ToString();
                    errorContent.text = numOfLogsError.ToString();



                }



                if (GUILayout.Button(logContent, (showLog) ? buttonActiveStyle : barStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    showLog = !showLog;
                    calculateCurrentLog();
                }

                if (GUILayout.Button(warningContent, (showWarning) ? buttonActiveStyle : barStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    showWarning = !showWarning;
                    calculateCurrentLog();
                }

                if (GUILayout.Button(errorContent, (showError) ? buttonActiveStyle : nonStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    showError = !showError;
                    calculateCurrentLog();
                }


                if (GUILayout.Button(closeContent, barStyle, GUILayout.Width(size), GUILayout.Height(size)))
                    show = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();



            GUILayout.Box(searchContent, barStyle, GUILayout.Width(size), GUILayout.Height(size));

            string newFilterText = GUILayout.TextField(filterText, searchStyle, GUILayout.Height(size),
                GUILayout.Width(Mathf.Max(200, Screen.width - 300 - size)));
            if (newFilterText != filterText)
            {
                filterText = newFilterText;
                calculateCurrentLog();
            }
            GUILayout.FlexibleSpace();
            var result = GUILayout.HorizontalSlider(size, 32, 100, GUILayout.Height(size), GUILayout.Width(Mathf.Max(size * 2.5f, 100)));
            if (result != size)
            {
                size = result;
                this.Initialize();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            GUILayout.EndArea();






        }



        void DrawLogs()
        {

            GUILayout.BeginArea(logsRect, backStyle);

            //GUI.skin = logScrollerSkin;
            //setStartPos();
            Vector2 drag = getDrag();

            if (drag.y != 0 && logsRect.Contains(new Vector2(downPos.x, Screen.height - downPos.y)))
            {
                scrollPosition.y += (drag.y - oldDrag);
            }
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            oldDrag = drag.y;


            int totalVisibleCount = (int)(Screen.height * 0.75f / size);
            int totalCount = currentLog.Count;

            totalVisibleCount = Mathf.Min(totalVisibleCount, totalCount - startIndex);
            int index = 0;
            int beforeHeight = (int)(startIndex * size);
            //selectedIndex = Mathf.Clamp( selectedIndex , -1 , totalCount -1);
            if (beforeHeight > 0)
            {
                //fill invisible gap before scroller to make proper scroller pos
                GUILayout.BeginHorizontal(GUILayout.Height(beforeHeight));
                GUILayout.Label("---");
                GUILayout.EndHorizontal();
            }

            int endIndex = startIndex + totalVisibleCount;
            endIndex = Mathf.Clamp(endIndex, 0, totalCount);
            bool scrollerVisible = (totalVisibleCount < totalCount);
            for (int i = startIndex; (startIndex + index) < endIndex; i++)
            {

                if (i >= currentLog.Count)
                    break;
                Log log = currentLog[i];

                if (log.logType == LogType.Log && !showLog)
                    continue;
                if (log.logType == LogType.Warning && !showWarning)
                    continue;
                if (log.logType == LogType.Error && !showError)
                    continue;
                if (log.logType == LogType.Assert && !showError)
                    continue;
                if (log.logType == LogType.Exception && !showError)
                    continue;

                if (index >= totalVisibleCount)
                {
                    break;
                }

                GUIContent content = null;
                if (log.logType == LogType.Log)
                    content = logContent;
                else if (log.logType == LogType.Warning)
                    content = warningContent;
                else
                    content = errorContent;
                content.text = string.Empty;
                //content.text = log.condition ;

                GUIStyle currentLogStyle = log == selectedLog ? selectedLogStyle : ((startIndex + index) % 2 == 0) ? evenLogStyle : oddLogStyle;

                var sample = samples[log.sampleId];

                GUILayout.BeginHorizontal(currentLogStyle);
                if (collapse)
                    GUILayout.Label(log.count.ToString(), barStyle, GUILayout.MaxWidth(80));
                GUILayout.Box(content, nonStyle, GUILayout.Width(size), GUILayout.Height(size));
                if (GUILayout.Button($"{sample.ToString("HH:mm:ss")} {log.condition}", logButtonStyle, GUILayout.ExpandWidth(true)))
                    selectedLog = selectedLog == log ? null : log;

                GUILayout.EndHorizontal();
                index++;
            }

            int afterHeight = (int)((totalCount - (startIndex + totalVisibleCount)) * size);
            if (afterHeight > 0)
            {
                //fill invisible gap after scroller to make proper scroller pos
                GUILayout.BeginHorizontal(GUILayout.Height(afterHeight));
                GUILayout.Label(" ");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            buttomRect.x = 0f;
            buttomRect.y = Screen.height - size;
            buttomRect.width = Screen.width;
            buttomRect.height = size;

            drawStack();

        }





        void drawStack()
        {

            if (selectedLog != null)
            {
                Vector2 drag = getDrag();
                if (drag.y != 0 && stackRect.Contains(new Vector2(downPos.x, Screen.height - downPos.y)))
                {
                    scrollPosition2.y += drag.y - oldDrag2;
                }
                oldDrag2 = drag.y;



                GUILayout.BeginArea(stackRect, backStyle);
                scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);
                var selectedSample = default(DateTime);
                try
                {
                    selectedSample = samples[selectedLog.sampleId];
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(selectedLog.condition, stackLabelStyle);
                GUILayout.EndHorizontal();
                GUILayout.Space(size * 0.25f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(selectedLog.stacktrace, stackLabelStyle);
                GUILayout.EndHorizontal();
                GUILayout.Space(size);
                GUILayout.EndScrollView();
                GUILayout.EndArea();


                GUILayout.BeginArea(buttomRect, backStyle);
                GUILayout.BeginHorizontal();





                /*GUILayout.Space( size.x );
				GUILayout.Box( graphContent ,nonStyle, GUILayout.Width(size.x) ,GUILayout.Height(size.y));
				GUILayout.Label( selectedLog.sampleId.ToString() ,nonStyle  );*/
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();



            }
            else
            {
                GUILayout.BeginArea(stackRect, backStyle);
                GUILayout.EndArea();
                GUILayout.BeginArea(buttomRect, backStyle);
                GUILayout.EndArea();
            }

        }


        void OnGUI()
        {

            if (!show)
            {
                return;
            }
            screenRect.x = 0;
            screenRect.y = 0;
            screenRect.width = Screen.width;
            screenRect.height = Screen.height;
            getDownPos();


            logsRect.x = 0f;
            logsRect.y = size * 2f;
            logsRect.width = Screen.width;
            logsRect.height = Screen.height * 0.75f - size * 2f;

            //stackRectTopLeft.x = 0f;
            stackRect.x = 0f;
            //stackRectTopLeft.y = Screen.height * 0.75f;
            stackRect.y = Screen.height * 0.75f;
            stackRect.width = Screen.width;
            stackRect.height = Screen.height * 0.25f - size;





            drawToolBar();
            DrawLogs();


        }

        List<Vector2> gestureDetector = new List<Vector2>();
        Vector2 gestureSum = Vector2.zero;
        float gestureLength = 0;
        int gestureCount = 0;
        bool isGestureDone()
        {
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touches.Length != 1)
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                }
                else
                {
                    if (Input.touches[0].phase == TouchPhase.Canceled || Input.touches[0].phase == TouchPhase.Ended)
                        gestureDetector.Clear();
                    else if (Input.touches[0].phase == TouchPhase.Moved)
                    {
                        Vector2 p = Input.touches[0].position;
                        if (gestureDetector.Count == 0 || (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                            gestureDetector.Add(p);
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonUp(0))
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                }
                else
                {
                    if (Input.GetMouseButton(0))
                    {
                        Vector2 p = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                        if (gestureDetector.Count == 0 || (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                            gestureDetector.Add(p);
                    }
                }
            }

            if (gestureDetector.Count < 10)
                return false;

            gestureSum = Vector2.zero;
            gestureLength = 0;
            Vector2 prevDelta = Vector2.zero;
            for (int i = 0; i < gestureDetector.Count - 2; i++)
            {

                Vector2 delta = gestureDetector[i + 1] - gestureDetector[i];
                float deltaLength = delta.magnitude;
                gestureSum += delta;
                gestureLength += deltaLength;

                float dot = Vector2.Dot(delta, prevDelta);
                if (dot < 0f)
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                    return false;
                }

                prevDelta = delta;
            }

            int gestureBase = (Screen.width + Screen.height) / 4;

            if (gestureLength > gestureBase && gestureSum.magnitude < gestureBase / 2)
            {
                gestureDetector.Clear();
                gestureCount++;
                if (gestureCount >= numOfCircleToShow)
                    return true;
            }

            return false;
        }



        Vector2 downPos;
        Vector2 getDownPos()
        {
            if (Application.platform == RuntimePlatform.Android ||
               Application.platform == RuntimePlatform.IPhonePlayer)
            {

                if (Input.touches.Length == 1 && Input.touches[0].phase == TouchPhase.Began)
                {
                    downPos = Input.touches[0].position;
                    return downPos;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    downPos.x = Input.mousePosition.x;
                    downPos.y = Input.mousePosition.y;
                    return downPos;
                }
            }

            return Vector2.zero;
        }
        //calculate drag amount , this is used for scrolling

        Vector2 mousePosition;
        Vector2 getDrag()
        {

            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touches.Length != 1)
                {
                    return Vector2.zero;
                }
                return Input.touches[0].position - downPos;
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    mousePosition = Input.mousePosition;
                    return mousePosition - downPos;
                }
                else
                {
                    return Vector2.zero;
                }
            }
        }

        //calculate the start index of visible log
        void calculateStartIndex()
        {
            startIndex = (int)(scrollPosition.y / size);
            startIndex = Mathf.Clamp(startIndex, 0, currentLog.Count);
        }








        void Update()
        {
            //fpsText = fps.ToString("0.000");
            //gcTotalMemory = (((float)System.GC.GetTotalMemory(false)) / 1024 / 1024);
            //addSample();


            //int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            //if (sceneIndex != -1 && string.IsNullOrEmpty(scenes[sceneIndex]))
            //    scenes[SceneManager.GetActiveScene().buildIndex] = SceneManager.GetActiveScene().name;



            calculateStartIndex();
            if (!show && isGestureDone())
            {
                show = true;
            }


            if (threadedLogs.Count > 0)
            {
                lock (threadedLogs)
                {
                    for (int i = 0; i < threadedLogs.Count; i++)
                    {
                        Log l = threadedLogs[i];
                        AddLog(l.condition, l.stacktrace, (LogType)l.logType);
                    }
                    threadedLogs.Clear();
                }
            }




        }



        void AddLog(string condition, string stacktrace, LogType type)
        {
            float memUsage = 0f;
            string _condition = "";
            if (cachedString.ContainsKey(condition))
            {
                _condition = cachedString[condition];
            }
            else
            {
                _condition = condition;
                cachedString.Add(_condition, _condition);
                memUsage += (string.IsNullOrEmpty(_condition) ? 0 : _condition.Length * sizeof(char));
                memUsage += System.IntPtr.Size;
            }
            string _stacktrace = "";
            if (cachedString.ContainsKey(stacktrace))
            {
                _stacktrace = cachedString[stacktrace];
            }
            else
            {
                _stacktrace = stacktrace;
                cachedString.Add(_stacktrace, _stacktrace);
                memUsage += (string.IsNullOrEmpty(_stacktrace) ? 0 : _stacktrace.Length * sizeof(char));
                memUsage += System.IntPtr.Size;
            }
            bool newLogAdded = false;

            samples.Add(System.DateTime.Now);
            Log log = new Log() { logType = (LogType)type, condition = _condition, stacktrace = _stacktrace, sampleId = samples.Count - 1 };
            memUsage += log.GetMemoryUsage();
            //memUsage += samples.Count * 13 ;



            bool isNew = false;
            //string key = _condition;// + "_!_" + _stacktrace ;
            if (logsDic.ContainsKey(_condition, stacktrace))
            {
                isNew = false;
                logsDic[_condition][stacktrace].count++;
            }
            else
            {
                isNew = true;
                collapsedLogs.Add(log);
                logsDic[_condition][stacktrace] = log;

                if (type == LogType.Log)
                    numOfCollapsedLogs++;
                else if (type == LogType.Warning)
                    numOfCollapsedLogsWarning++;
                else
                    numOfCollapsedLogsError++;
            }

            if (type == LogType.Log)
                numOfLogs++;
            else if (type == LogType.Warning)
                numOfLogsWarning++;
            else
                numOfLogsError++;


            logs.Add(log);
            if (!collapse || isNew)
            {
                bool skip = false;
                if (log.logType == LogType.Log && !showLog)
                    skip = true;
                if (log.logType == LogType.Warning && !showWarning)
                    skip = true;
                if (log.logType == LogType.Error && !showError)
                    skip = true;
                if (log.logType == LogType.Assert && !showError)
                    skip = true;
                if (log.logType == LogType.Exception && !showError)
                    skip = true;

                if (!skip)
                {
                    if (string.IsNullOrEmpty(filterText) || log.condition.ToLower().Contains(filterText.ToLower()))
                    {
                        currentLog.Add(log);
                        newLogAdded = true;
                    }
                }
            }

            if (newLogAdded)
            {
                calculateStartIndex();
                int totalCount = currentLog.Count;
                int totalVisibleCount = (int)(Screen.height * 0.75f / size);
                if (startIndex >= (totalCount - totalVisibleCount))
                    scrollPosition.y += size;
            }


        }

        List<Log> threadedLogs = new List<Log>();




    }

}
