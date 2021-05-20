using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PS3TrophyIsGood
{
   
    public partial class CopyFrom : Form
    {
        /// <summary>
        /// Class for havian a pair for use it later (you could do it generic as c++ Pair but to lazy
        /// </summary>
        public class Pair
        {
            public int Id { get; set; }
            public long Date { get; set; }
            public Pair(int id, long date)
            {
                Id = id;
                Date = date;
            }
        }

        private DateTime lastSyncTrophyTime;

        public CopyFrom(DateTime lastSyncTrophyTime)
        {
            InitializeComponent();
            this.lastSyncTrophyTime = lastSyncTrophyTime;
            groupBox1.Visible = false;
        }

        private IEnumerable<long> _times;
        public List<long> Times
        {
            get
            {
                if (_times == null)
                    return new List<long>();
                return _times.ToList();
            }
        }

        /// <summary>
        /// This get the timestamp from a profile(asuming is a legit one) then modify them to looks like they are legit but not a comple copy
        /// </summary>
        /// <returns></returns>
        private IEnumerable<long> smartCopy()
        {
            var trophies = copyFrom(textBox1.Text).ToList();
            trophies.Sort((a, b) => a.Date.CompareTo(b.Date));
            var rand = new Random();
            DateTime dtTrophy = new DateTime();
            for (int i = 0; i < trophies.Count; i++)
            {
                if (trophies[i].Date == 0) continue;
                DateTime dtAux = trophies[i].Date.TimeStampToDateTime();
                dtAux = dtAux.AddYears((int)yearsNumeric.Value).AddMonths((int)monthNumeric.Value).AddHours((int)hoursNumeric.Value).AddDays((double)daysNumeric.Value);
                do
                {
                    DateTime dtAux2 = dtAux.AddSeconds(rand.Next((int)minMinutes.Value * 60, (int)maxMinutes.Value * 60));
                    if (DateTime.Compare(dtAux2, dtTrophy) >= 0)
                        dtAux = dtAux2;
                } while (DateTime.Compare(dtTrophy, dtAux) > 0);
                dtTrophy = dtAux;
                trophies[i].Date = dtTrophy.DateTimeToTimeStamp();
            }

            trophies.Sort((a, b) => a.Id.CompareTo(b.Id));
            return trophies.Select(d=>d.Date);
        }

        private IEnumerable<long> copyFrom()
        {
            var trophies = copyFrom(textBox1.Text).ToList();
            if (hoursNumeric.Value != 0) {
                for (int i = 0; i < trophies.Count; i++)
                {
                    if (trophies[i].Date == 0) continue;
                    DateTime dtAux = trophies[i].Date.TimeStampToDateTime().AddHours((int)hoursNumeric.Value);
                    trophies[i].Date = dtAux.DateTimeToTimeStamp();
                }
            }
            return trophies.Select(d => d.Date);
        }

        /// <summary>
        /// Just parse and get the timestamps from a profile from https://psntrophyleaders.com
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IEnumerable<Pair> copyFrom(string url)
        {
            int i = 0;
            Regex regex = new Regex("<td class=\"date_earned\">\\s+<span class=\"sort\">\\d+</span>");
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent: Other");
                var x = regex.Matches(client.DownloadString(url));
                foreach (Match match in x)
                    yield return new Pair(i++,long.Parse(Regex.Match(match.Value, "\\d+").ToString()));
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) groupBox1.Visible = true;
            else
            {
                groupBox1.Visible = false;
                daysNumeric.Value = 0;
                monthNumeric.Value = 0;
                yearsNumeric.Value = 0;
                minMinutes.Value = 0;
                maxMinutes.Value = 0;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (minMinutes.Value > maxMinutes.Value)
                MessageBox.Show(Properties.strings.MinCantBeGreaterThanMax);
            else if (Regex.IsMatch(textBox1.Text,"https://psntrophyleaders.com/user/view/" + "\\S+/\\S+"))
            {
                _times = checkBox1.Checked ? smartCopy() : copyFrom();
                var hasDateGreaterThanCurrent = _times.Any(t => DateTime.Compare(t.TimeStampToDateTime(), DateTime.Now) > 0);
                var hasDateLowerThanLastSync = _times.Any(t => t > 0 && DateTime.Compare(lastSyncTrophyTime, t.TimeStampToDateTime()) > 0);

                if (hasDateGreaterThanCurrent && hasDateLowerThanLastSync)
                {
                    if ((MessageBox.Show(Properties.strings.CopyHasDateLowerThanLastSync, Properties.strings.Danger, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        && (MessageBox.Show(Properties.strings.CopyHasDateGreaterThanCurrent, Properties.strings.Danger, MessageBoxButtons.YesNo) == DialogResult.Yes))
                        DialogResult = DialogResult.OK;
                }
                else if (hasDateGreaterThanCurrent)
                {
                    if (MessageBox.Show(Properties.strings.CopyHasDateGreaterThanCurrent, Properties.strings.Danger, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        DialogResult = DialogResult.OK;
                }
                else if (hasDateLowerThanLastSync)
                {
                    if (MessageBox.Show(Properties.strings.CopyHasDateLowerThanLastSync, Properties.strings.Danger, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        DialogResult = DialogResult.OK;
                }
                else
                    DialogResult = DialogResult.OK;
            }
            else MessageBox.Show(Properties.strings.CantFindGame);
        }
    }
}
