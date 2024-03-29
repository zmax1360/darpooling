﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserNodeGUI
{
    public partial class MainWindow : Form
    {
        UserNodeCore.UserNodeCore core;
        ConnectDialog connectDlg;
        RegisterUserDialog registerDlg;

        public MainWindow()
        {
            InitializeComponent();
            core = new UserNodeCore.UserNodeCore(new Communication.UserNode("prova"));
            core.resultCallback += new UserNodeCore.UserNodeCore.ResultReceiveHandler(onNewResult);
        }

        /// <summary>
        /// Each new Result received (through callback in NodeCore), goes
        /// here, then is processed in an appropriate method, exploiting
        /// overloading to change behaviour depending on Result subclass.
        /// </summary>
        /// <param name="result"></param>
        public void onNewResult(Communication.Result result) {
            Type type = result.GetType();
            if (type == typeof(Communication.SearchTripResult))
            {
                Communication.SearchTripResult sr = (Communication.SearchTripResult) result;
                ((ResultTabPage)ResultTabControl.TabPages[sr.OriginalQueryID]).setGridviewDatasource(sr.Trips);
                connectedStatusLabel.Text = "Connected";
            }
            else if (type == typeof(Communication.InsertOkResult))
            {
                connectedStatusLabel.Text = "Connected - Trip inserted";
            }
            else if (type == typeof(Communication.LoginOkResult))
            {
                SetConnectedView(true);
                connectDlg.Dispose();
            }
            else if (type == typeof(Communication.RegisterOkResult))
            {
                SetConnectedView(true);
                registerDlg.Dispose();
            }
            else if (type == typeof(Communication.LoginErrorResult) ||
                type == typeof(Communication.RegisterErrorResult) ||
                type == typeof(Communication.InsertErrorResult))
            {
                MessageBox.Show("Error. Something went wrong, try again.");
            }
        }
                
        # region MenuItems event

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            connectDlg = new ConnectDialog(core);           
            connectDlg.SetConnectedViewCallback = new ConnectDialog.SetConnectedViewDelegate(this.SetConnectedView);
            connectDlg.ShowDialog();
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            core.Unjoin();
            SetConnectedView(false);
        }


        private void newTripToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewTripDialog dlg = new NewTripDialog(core);
            dlg.ShowDialog();
        }

        private void registerUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            registerDlg = new RegisterUserDialog(core);
            registerDlg.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        /*
         * Main interface methods
         */

        private void searchButton_Click(object sender, EventArgs e)
        {
            /*
             * 1. Build a new search trip request and send it.
             * 2. Create a new tab
             * 3. On receiving the result, populate the tab with trips.
             */
            Communication.QueryBuilder qb = new Communication.QueryBuilder()
            {
                Owner = core.UserNode.User.UserName,
                DepartureName = sourceTextBox.Text,
                ArrivalName = destinationTextBox.Text,
                Range = (int) rangeUpDown.Value
            };

            string source = sourceTextBox.Text;
            string destination = destinationTextBox.Text;
            AddTabPage(qb.ID, source + " - " + destination);
            core.SearchTrip(qb);
        }

        private void searchButton_UpdateStatus(object sender, EventArgs e)
        {
            if (sourceTextBox.Text.Length != 0 &&
                destinationTextBox.Text.Length != 0)
                searchButton.Enabled = true;
            else
                searchButton.Enabled = false;
        }

        private void closeTabButton_Click(object sender, EventArgs e)
        {
            //ResultTabControl.Controls.RemoveAt();
            TabPage p = ResultTabControl.SelectedTab;
            if (p != null)
                p.Dispose();
        }

        /*
         * Helper methods
         */

        private void SetConnectedView(bool connected)
        {
            if (connected)
            {
                connectedStatusLabel.Text = "Connected";
                disconnectToolStripMenuItem.Enabled = true;
                connectToolStripMenuItem.Enabled = false;
                newTripToolStripMenuItem.Enabled = true;
                MainPanel.Show();
                ResultTabControl.Show();
            }
            else
            {
                connectedStatusLabel.Text = "Not connected";
                connectToolStripMenuItem.Enabled = true;
                disconnectToolStripMenuItem.Enabled = false;
                newTripToolStripMenuItem.Enabled = false;
                MainPanel.Hide();
                ResultTabControl.Hide();
            }
        }

        private void AddTabPage(string key, string label)
        {
            TabPage page = new ResultTabPage();
            page.Name = key;
            page.Text = label;
            ResultTabControl.TabPages.Add(page);
            if (page != ResultTabControl.TabPages[key])
                throw new Exception();
            ResultTabControl.SelectTab(page);
        }
    }
}