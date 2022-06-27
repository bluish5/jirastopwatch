/**************************************************************************
Copyright 2016 Carsten Gehling

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**************************************************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StopWatch
{
    public partial class WorklogForm : Form
    {
        #region public members
        public string Comment
        {
            get
            {
                return tbComment.Text;
            }
        }
        public EstimateUpdateMethods estimateUpdateMethod
        {
            get
            {
                return _estimateUpdateMethod;
            }
        }
        public string EstimateValue
        {
            get
            {
                switch(this.estimateUpdateMethod)
                {
                    case EstimateUpdateMethods.SetTo:                       
                        return this.tbSetTo.Text;
                    case EstimateUpdateMethods.ManualDecrease:
                        return this.tbReduceBy.Text;
                    case EstimateUpdateMethods.Auto:
                    case EstimateUpdateMethods.Leave:
                    default:
                        return null;                        
                }
            }
        }
        public DateTimeOffset InitialStartTime
        {
            get
            {
                return this.startDatePicker.Value.Date + this.startTimePicker.Value.TimeOfDay;
            }
        }
        public string RemainingEstimate
        {
            get
            {
                return _RemainingEstimate;
            }
            set
            {
                _RemainingEstimate = value;
                RemainingEstimateUpdated();
            }
        }

        public int RemainingEstimateSeconds
        {
            get
            {
                return _RemainingEstimateSeconds;
            }
            set
            {
                _RemainingEstimateSeconds = value;
                RemainingEstimateUpdated();
            }
        }

        public IEnumerable<Issue> AvailableIssues
        {
            set
            {
                cbSubproject.Items.Clear();
                foreach (var issue in value)
                    cbSubproject.Items.Add(new CBIssueItem(issue.Key, issue.Fields.Summary));
            }
        }

        public string SubprojectKey {
            get
            {
                return cbSubproject.Text;
            }
        }
        #endregion

        #region private members
        private JiraClient jiraClient;
        private int keyWidth;
        private ComboTextBoxEvents cbJiraTbEvents;
        #endregion

        #region private classes
        // content item for the combo box
        private class CBIssueItem
        {
            public string Key { get; set; }
            public string Summary { get; set; }

            public CBIssueItem(string key, string summary)
            {
                Key = key;
                Summary = summary;
            }
        }
        #endregion

        #region public methods
        public WorklogForm(DateTimeOffset startTime, TimeSpan TimeElapsed, string subprojectKey, string comment, EstimateUpdateMethods estimateUpdateMethod, string estimateUpdateValue, JiraClient jiraClient)
        {            
            this.TimeElapsed = TimeElapsed;
            DateTimeOffset initialStartTime;
            if (startTime == null)
            {
                initialStartTime = DateTimeOffset.UtcNow.Subtract(TimeElapsed);
            }else
            {
                initialStartTime = startTime;
            }
            InitializeComponent();
            if (!String.IsNullOrEmpty(subprojectKey))
            {
                cbSubproject.Text = subprojectKey;
            }
            if (!String.IsNullOrEmpty(comment))
            {
                tbComment.Text = comment;
                tbComment.SelectionStart = 0;
            }

            // I don't see why I need to do this, but the first time I call LocalDateTime it seems to change time zone on the actual Date4TimeOffset
            // So I don't get the right time.  So I call just once and update both from the same object
            DateTime localInitialStartTime = initialStartTime.LocalDateTime;
            this.startDatePicker.Value = localInitialStartTime;
            this.startTimePicker.Value = localInitialStartTime;

            switch ( estimateUpdateMethod ) {
                case EstimateUpdateMethods.Auto:
                    rdEstimateAdjustAuto.Checked = true;
                    break;
                case EstimateUpdateMethods.Leave:
                    rdEstimateAdjustLeave.Checked = true;
                    break;
                case EstimateUpdateMethods.SetTo:
                    rdEstimateAdjustSetTo.Checked = true;
                    tbSetTo.Text = estimateUpdateValue;
                    break;
                case EstimateUpdateMethods.ManualDecrease:
                    rdEstimateAdjustManualDecrease.Checked = true;
                    tbReduceBy.Text = estimateUpdateValue;
                    break;
            }
            this.jiraClient = jiraClient;

            cbJiraTbEvents = new ComboTextBoxEvents(cbSubproject);
            cbJiraTbEvents.Paste += cbJiraTbEvents_Paste;
        }
        #endregion

        #region private methods
        private void LoadIssues()
        {
            string jql = "issuetype = Commessa AND statusCategory = 4 ORDER BY key ASC, created DESC";

            Task.Factory.StartNew(
                () =>
                {
                    List<Issue> availableIssues = jiraClient.GetIssuesByJQL(jql).Issues;

                    if (availableIssues == null)
                        return;

                    this.InvokeIfRequired(
                        () =>
                        {
                            AvailableIssues = availableIssues;
                            cbSubproject.DropDownHeight = 120;
                            cbSubproject.Invalidate();
                        }
                    );
                }
            );
        }
        #endregion

        #region private fields

        /// <summary>
        /// Update method for the estimate
        /// </summary>
        private EstimateUpdateMethods _estimateUpdateMethod = EstimateUpdateMethods.Auto;
        private bool tbSetToInvalid = false;
        private bool tbReduceByInvalid = false;
        private string _RemainingEstimate;
        private int _RemainingEstimateSeconds;
        private TimeSpan TimeElapsed;
        #endregion

        #region private eventhandlers
        private void tbComment_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfCtrlEnter(e);
        }

        private void tbSetTo_KeyUp(object sender, KeyEventArgs e)
        {
            if (tbSetToInvalid)
            {
                ValidateTimeInput(tbSetTo, false);
            }
        }
        private void tbSetTo_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfEnter(e);
        }
        private void tbReduceBy_KeyUp(object sender, KeyEventArgs e)
        {
            if (tbReduceByInvalid)
            {
                ValidateTimeInput(tbReduceBy, false);
            }
        }
        private void tbReduceBy_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfEnter(e);
        }
        private void rdEstimateAdjustAuto_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfEnter(e);
        }
        private void rdEstimateAdjustLeave_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfEnter(e);
        }
        private void rdEstimateAdjustSetTo_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfEnter(e);
        }
        private void rdEstimateAdjustManualDecrease_KeyDown(object sender, KeyEventArgs e)
        {
            SubmitIfEnter(e);
        }
        private void tbSetTo_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Valdiate but do not set the cancel event
            // The reason for this is that setting the cancel event means you can't leave the field,
            // even to choose a different estimate adjustment option.
            // So valiate (so the colour updates) but do not cancel
            ValidateTimeInput(tbSetTo, false);
        }
        private void tbReduceBy_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Valdiate but do not set the cancel event
            // The reason for this is that setting the cancel event means you can't leave the field,
            // even to choose a different estimate adjustment option.
            // So valiate (so the colour updates) but do not cancel
            ValidateTimeInput(tbReduceBy, false);
        }

        private void SubmitIfCtrlEnter(KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.Enter))
                SubmitForm();
        }

        private void SubmitIfEnter(KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Enter))
                SubmitForm();
        }

        private void SubmitForm()
        {
            DialogResult = DialogResult.OK;
            PostTimeAndClose();
            if (DialogResult == DialogResult.OK)
                Close();
        }

        private void estimateUpdateMethod_changed(object sender, EventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (button != null && button.Checked)
            {
                switch (button.Name)
                {
                    case "rdEstimateAdjustAuto":
                        this._estimateUpdateMethod = EstimateUpdateMethods.Auto;
                        this.tbSetTo.Enabled = false;
                        this.tbSetTo.BackColor = SystemColors.Window;
                        this.tbReduceBy.Enabled = false;
                        this.tbReduceBy.BackColor = SystemColors.Window;
                        break;
                    case "rdEstimateAdjustLeave":
                        this._estimateUpdateMethod = EstimateUpdateMethods.Leave;
                        this.tbSetTo.Enabled = false;
                        this.tbSetTo.BackColor = SystemColors.Window;
                        this.tbReduceBy.Enabled = false;
                        this.tbReduceBy.BackColor = SystemColors.Window;
                        break;
                    case "rdEstimateAdjustSetTo":
                        this._estimateUpdateMethod = EstimateUpdateMethods.SetTo;
                        this.tbSetTo.Enabled = true;
                        this.tbReduceBy.Enabled = false;
                        this.tbReduceBy.BackColor = SystemColors.Window;
                        break;
                    case "rdEstimateAdjustManualDecrease":
                        this._estimateUpdateMethod = EstimateUpdateMethods.ManualDecrease;
                        this.tbSetTo.Enabled = false;
                        this.tbSetTo.BackColor = SystemColors.Window;
                        this.tbReduceBy.Enabled = true;                        
                        break;
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            PostTimeAndClose();
        }

        private void PostTimeAndClose()
        {
            if (!ValidateAllInputs())
            {
                DialogResult = DialogResult.None;
                return;
            }            
        }

        void cbJira_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index == 0)
                keyWidth = 0;
            CBIssueItem item = (CBIssueItem)cbSubproject.Items[e.Index];
            Font font = new Font(cbSubproject.Font.FontFamily, cbSubproject.Font.Size * 0.8f, cbSubproject.Font.Style);
            Size size = TextRenderer.MeasureText(e.Graphics, item.Key, font);
            e.ItemHeight = size.Height;
            if (keyWidth < size.Width)
                keyWidth = size.Width;
        }


        void cbJira_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Draw the default background
            e.DrawBackground();

            CBIssueItem item = (CBIssueItem)cbSubproject.Items[e.Index];

            // Create rectangles for the columns to display
            Rectangle r1 = e.Bounds;
            Rectangle r2 = e.Bounds;

            r1.Width = keyWidth;

            r2.X = r1.Width + 5;
            r2.Width = 500 - keyWidth;

            Font font = new Font(e.Font.FontFamily, e.Font.Size * 0.8f, e.Font.Style);

            // Draw the text on the first column
            using (SolidBrush sb = new SolidBrush(e.ForeColor))
                e.Graphics.DrawString(item.Key, font, sb, r1);

            // Draw a line to isolate the columns 
            using (Pen p = new Pen(Color.Black))
                e.Graphics.DrawLine(p, r1.Right, 0, r1.Right, r1.Bottom);

            // Draw the text on the second column
            using (SolidBrush sb = new SolidBrush(e.ForeColor))
                e.Graphics.DrawString(item.Summary, font, sb, r2);

            // Draw a line to isolate the columns 
            using (Pen p = new Pen(Color.Black))
                e.Graphics.DrawLine(p, r1.Right, 0, r1.Right, 140);

        }


        private void cbJira_DropDown(object sender, EventArgs e)
        {
            LoadIssues();
        }


        private void cbJiraTbEvents_Paste(object sender, EventArgs e)
        {
            PasteKeyFromClipboard();
        }

        public void PasteKeyFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                cbSubproject.Text = JiraKeyHelpers.ParseUrlToKey(Clipboard.GetText());
            }
        }

        public void CopyKeyToClipboard()
        {
            if (string.IsNullOrEmpty(cbSubproject.Text))
                return;
            Clipboard.SetText(cbSubproject.Text);
        }
        #endregion

        #region private utility methods

        private void RemainingEstimateUpdated()
        {
            if (string.IsNullOrWhiteSpace(RemainingEstimate))
            {
                rdEstimateAdjustLeave.Text = "&Leave Unchanged";
            }
            else
            {
                rdEstimateAdjustLeave.Text = string.Format("&Leave As {0}", _RemainingEstimate);
            }

            if( TimeElapsed != null && RemainingEstimateSeconds > 0){
                rdEstimateAdjustAuto.Text = string.Format("Adjust &Automatically (to {0})", calculatedAdjustedRemainingEstimate());
            }else {
                rdEstimateAdjustAuto.Text = "Adjust &Automatically";
            }
        }

        private string calculatedAdjustedRemainingEstimate()
        {
            
            int AdjustedRemainingSeconds = RemainingEstimateSeconds - (int)Math.Floor(TimeElapsed.TotalSeconds);
            if (AdjustedRemainingSeconds > 0)
            {
                TimeSpan AdjustedRemaining = new TimeSpan(0, 0, AdjustedRemainingSeconds);
                return JiraTimeHelpers.TimeSpanToJiraTime(AdjustedRemaining);
            }
            else
            {
                return "0m";
            }
        }

        private void foo(string text)
        {

        }

        /// <summary>
        /// Validates the required inputs.  Returns
        /// </summary>
        /// <returns></returns>
        private bool ValidateAllInputs()
        {
            Boolean AllValid = true;
            tbSetToInvalid = false;
            tbReduceByInvalid = false;
            switch(estimateUpdateMethod) {
                case EstimateUpdateMethods.SetTo: 
                    if (!ValidateTimeInput(tbSetTo, true))
                    {
                        AllValid = false;                        
                    }
                    break;
                case EstimateUpdateMethods.ManualDecrease:
                    if (!ValidateTimeInput(tbReduceBy, true))
                    {
                        AllValid = false;                        
                    }
                    break;
            }

            return AllValid;
        }
        /// <summary>
        /// Checks if the time entered in the submitted textbox is valid
        /// Marks it as invalid if it is not
        /// </summary>
        /// <param name="tb"></param>
        /// <returns></returns>
        private bool ValidateTimeInput(TextBox tb, bool FocusIfInvalid)
        {
            bool fieldIsValid;
            if (tb.Enabled)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.BackColor = Color.Tomato;
                    if (FocusIfInvalid)
                    {
                        tb.Select();
                    }
                    fieldIsValid = false;
                }
                else
                {
                    TimeSpan? time = JiraTimeHelpers.JiraTimeToTimeSpan(tb.Text);
                    if (time == null)
                    {
                        tb.BackColor = Color.Tomato;
                        if (FocusIfInvalid)
                        {
                            tb.Select(0, tb.Text.Length);
                        }
                        fieldIsValid = false;
                    }
                    else{
                        tb.BackColor = SystemColors.Window;
                        fieldIsValid = true;
                    }
                }
            } 
            else 
            {
                fieldIsValid = true;
            }

            switch(tb.Name)
            {
                case "tbSetTo":
                    tbSetToInvalid = !fieldIsValid;
                    break;
                case "tbReduceBy":
                    tbReduceByInvalid = !fieldIsValid;
                    break;
            }
            return fieldIsValid;
        }
        #endregion
    }
}
