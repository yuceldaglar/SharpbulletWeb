using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBullet.Web.System
{
    public class SbProfiler
    {
        public DateTime CreateTime = DateTime.Now;

        public List<SbProfilerItem> Measurements = new List<SbProfilerItem>();

        public void Start(string description)
        {
            var measurement = new SbProfilerItem() { Description = description };
            Measurements.Add(measurement);
        }

        public void Stop()
        {
            Measurements.Last().WriteDuration();
        }

        public void Restart(string description)
        {
            Stop();
            Start(description);
        }

        public void Measure(string description, Action action)
        {
            Start(description);

            action();

            Stop();  
        }

        public int GetTotalDuration()
        {
            Stop();

            if (Measurements == null) return 0;

            int result = 0;
            foreach (var item in Measurements)
            {
                result += item.Duration;
            }

            return result;
        }
    }

    public class SbProfilerItem
    {
        public DateTime StartTime;
        public int Duration;
        public string Description;

        public SbProfilerItem()
        {
            StartTime = DateTime.Now;
        }

        public void WriteDuration()
        {
            Duration = DateTime.Now.Subtract(StartTime).Milliseconds;
        }
    }
}
