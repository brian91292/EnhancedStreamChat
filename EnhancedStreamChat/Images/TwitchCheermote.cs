using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnhancedStreamChat.Images
{
    public class CheermoteTier
    {
        public int minBits = 0;
        public string color = "";
        public bool canCheer = false;
    }

    public class TwitchCheermote
    {
        public List<CheermoteTier> tiers = new List<CheermoteTier>();

        public string GetColor(int numBits)
        {
            for (int i = 1; i < tiers.Count; i++)
            {
                if (numBits < tiers[i].minBits)
                    return tiers[i - 1].color;
            }
            return tiers[0].color;
        }

        public string GetTier(int numBits)
        {
            for (int i = 1; i < tiers.Count; i++)
            {
                if (numBits < tiers[i].minBits)
                    return tiers[i - 1].minBits.ToString();
            }
            return tiers[0].minBits.ToString();
        }
    }
}
