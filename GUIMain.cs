﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS2StatTracker
{
    public partial class GUIMain : Form
    {
        // Update this with new versions.
        string VERSION_NUM = "0.4.X";
        string PROGRAM_TITLE = "Real Time Stat Tracker";
        List<EventLog> m_eventLog;
        Dictionary<string,
            Weapon> m_sessionWeapons;
        Dictionary<string,
            string> m_itemCache;            // Cache of item IDs to their name.
        Dictionary<string, Player>
            m_playerCache;                  // Cache of player IDs to their struct. 
        EventLog m_currentEvent;
        Player m_player;
        Player m_startPlayer;
        string m_userID;
        float m_sessionStartHSR;            // Start of session headshot ratio.
        float m_sessionStartKDR;
        int m_activeSeconds;
        int m_timeout;
        bool m_lastEventFound;
        bool m_sessionStarted;
        bool m_weaponsUpdated;              // Set to true when weapons are first updated. False on disconnect.
        bool m_initialized;
        bool m_pageLoaded;
        bool m_dragging;
        bool m_resizing;
        Color m_highColor;
        Color m_lowColor;
        Point m_dragCursorPoint;
        Point m_dragFormPoint;
        public GUIMain()
        {
            InitializeComponent();
            // Load version.
            this.versionLabel.Text = PROGRAM_TITLE + " V " + VERSION_NUM;
            m_highColor = Color.FromArgb(0, 192, 0);
            m_lowColor = Color.Red;
            m_eventLog = new List<EventLog>();
            m_sessionWeapons = new Dictionary<string, Weapon>();
            m_playerCache = new Dictionary<string, Player>();
            m_itemCache = new Dictionary<string, string>();
            m_currentEvent = new EventLog();
            m_player = new Player();
            m_startPlayer = new Player();
            m_userID = "";
            m_sessionStarted = false;
            m_lastEventFound = false;
            m_initialized = false;
            m_pageLoaded = false;
            m_weaponsUpdated = false;
            m_dragging = false;
            m_resizing = false;
            m_activeSeconds = 0;
            m_timeout = 0;
            m_sessionStartHSR = 0.0f;
            m_sessionStartKDR = 0.0f;

            // Prevent X images showing up.
            ((DataGridViewImageColumn)this.eventLogGridView.Columns[2]).DefaultCellStyle.NullValue = null;

            // Handle mouse movement and resizing on borderless window.
            this.resizePanelLR.MouseDown += OnMouseDownResize;
            this.resizePanelLR.MouseMove += OnMouseMove;
            this.resizePanelLR.MouseUp += OnMouseUp;

            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.GetType() == typeof(Label))
                {
                    ctrl.MouseMove += OnMouseMove;
                    ctrl.MouseDown += OnMouseDown;
                    ctrl.MouseUp += OnMouseUp;
                }
            }

            this.menuStrip1.MouseMove += OnMouseMove;
            this.menuStrip1.MouseDown += OnMouseDown;
            this.menuStrip1.MouseUp += OnMouseUp;

            this.panel1.MouseMove += OnMouseMove;
            this.panel1.MouseDown += OnMouseDown;
            this.panel1.MouseUp += OnMouseUp;

            foreach (Control ctrl in this.panel1.Controls)
            {
                if (ctrl.GetType() == typeof(Label))
                {
                    ctrl.MouseMove += OnMouseMove;
                    ctrl.MouseDown += OnMouseDown;
                    ctrl.MouseUp += OnMouseUp;
                }
            }
            this.panel2.MouseMove += OnMouseMove;
            this.panel2.MouseDown += OnMouseDown;
            this.panel2.MouseUp += OnMouseUp;

            foreach (Control ctrl in this.panel2.Controls)
            {
                if (ctrl.GetType() == typeof(Label))
                {
                    ctrl.MouseMove += OnMouseMove;
                    ctrl.MouseDown += OnMouseDown;
                    ctrl.MouseUp += OnMouseUp;
                }
            }

            m_currentEvent.Initialize();
        }

        private void ClearUsers()
        {
            usernameTextBox.Items.Clear();
        }

        //////////////////////////////////
        // Button clicks.
        //////////////////////////////////

        private void button1_Click(object sender, EventArgs e)
        {
            Initialize();
        }

        private void startSessionButton_Click(object sender, EventArgs e)
        {
            StartSession();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            m_activeSeconds += (timer1.Interval / 1000);

            GetEventStats();

            // Update weapons every 30 minutes. Currently hardcoded. May eventually add sliders under options.
            // Also may eventually add options.
            if(m_activeSeconds % 1800 == 0)
                GetPlayerWeapons();
        }

        //////////////////////////////////
        // Mouse handlers
        //////////////////////////////////
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            m_dragging = true;
            m_resizing = false;
            m_dragCursorPoint = Cursor.Position;
            m_dragFormPoint = this.Location;
        }

        private void OnMouseDownResize(object sender, MouseEventArgs e)
        {
            m_dragging = false;
            m_resizing = true;
            m_dragCursorPoint = Cursor.Position;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (m_dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(m_dragCursorPoint));
                this.Location = Point.Add(m_dragFormPoint, new Size(dif));
            }

            if (m_resizing)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(m_dragCursorPoint));
                this.Size += new Size(dif);
                m_dragCursorPoint = Cursor.Position;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            m_dragging = false;
            m_resizing = false;
        }

        //////////////////////////////////
        // Tool strip button clicks.
        //////////////////////////////////

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void updateEventsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(m_sessionStarted)
                GetEventStats();
        }

        private void updateWeaponsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_sessionStarted)
                GetPlayerWeapons();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GUIOptions config = new GUIOptions();
            config.ShowDialog(this);
        }

        private void clearUsersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearUsers();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GUIAbout about = new GUIAbout();
            about.SetTitle(PROGRAM_TITLE);
            about.SetVersion("Version " + VERSION_NUM);
            about.ShowDialog(this);
        }

        private void positiveColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.colorDialog1.AllowFullOpen = true;
            if(this.colorDialog1.ShowDialog(this) == DialogResult.OK)
                m_highColor = this.colorDialog1.Color;
        }

        private void negativeColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.colorDialog1.AllowFullOpen = true;
            if (this.colorDialog1.ShowDialog(this) == DialogResult.OK)
            m_lowColor = this.colorDialog1.Color;
        }

        private void cancelOperationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CancelOperation();
        }
    }
}