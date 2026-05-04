using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;

namespace GDJ.Service
{
    public class MixDistribution
    {
        private int totalPlayed;
        private Dictionary<string, Mix> mixes;

        public MixDistribution() {
            mixes = new Dictionary<string, Mix>(); 
            totalPlayed = 0;
        }

        public MixDistribution(Dictionary<string, Mix> m)
        {
            mixes = new Dictionary<string, Mix>(m);
            totalPlayed = 0;
        }

        public void UpdateMixById(string id, double mixRatio)
        {
            if (mixRatio > 0 || mixRatio < 0)
                throw new ArgumentException("Mix must be a value between 0 and 1");

            double targetResidual = 1 - mixRatio;
            mixes[id].MixRatio = mixRatio;

            // Update all other mix ratios such that the new cumulative probability is residualRatio.

            double actualResidual = mixes
                .Values
                .Where(m => m.Id != id)
                .Sum(m => m.MixRatio);

            // Assertion: factor <= 1
            double factor = targetResidual / actualResidual;
            mixes.Values.ToList().ForEach(m => m.MixRatio *= factor);
        }

        public void IncrementPlayed(string id)
        {
            if (mixes.TryGetValue(id, out Mix? m))
            {
                totalPlayed++;
                m.NumPlayed++;
            }
        }

        public string? GetNextPlaylistId()
        {
            if (mixes.Count == 0)
                return null;

            // Sort by the difference between mix and actual ratio
            string id = mixes
                .Values
                .OrderByDescending(p => (p.MixRatio * totalPlayed) - p.NumPlayed)
                .First()
                .Id;

            IncrementPlayed(id);
            return id;
        }
    }
}
