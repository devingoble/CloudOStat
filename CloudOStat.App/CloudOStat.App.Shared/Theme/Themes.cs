using System;
using System.Collections.Generic;
using System.Text;

namespace CloudOStat.App.Shared.Theme;

public static class SmokerAppColorSchemes
{
    public class SmokerEmber
    {
        // Main palette
        public const string Primary = "#B33F1C";      // Smoked Ember
        public const string Secondary = "#5A2E1A";    // Charred Oak
        public const string Accent = "#F2A65A";       // Glazed Honey
        public const string Background = "#F7F3EE";   // Ash White

        // Status colors
        public const string Heating = "#D64826";      // Active Flame (brighter, more energetic red-orange)
        public const string Cooling = "#7A8B99";      // Cooling Ash (blue-grey, clearly cooler)
        public const string OnTemp = "#6C8F3D";       // Herb Green (success, maintain temp)
        public const string Warning = "#E8B339";      // Amber Warning (more yellow, distinct caution)
        public const string Error = "#8B1E14";        // Deep Red Ember (danger, critical)
    }

    public class BackyardPitmaster
    {
        // Main palette
        public const string Primary = "#2F3E46";      // Smoker Steel
        public const string Secondary = "#354F52";    // Cold Smoke
        public const string Accent = "#DDA15E";       // Seasoned Wood
        public const string Background = "#EAE7DC";   // Canvas Tan

        // Status colors
        public const string Heating = "#E8A547";      // Glowing Coals (brighter, warmer orange)
        public const string Cooling = "#5B7C8D";      // Cool Steel Blue (lighter, bluer than primary)
        public const string OnTemp = "#6B8E23";       // Olive Green (success, stable temp)
        public const string Warning = "#BC4749";      // Overtemp Red (caution, too hot)
        public const string Error = "#8B1E1E";        // Deep Red (danger, critical failure)
    }

    public class TemperatureGradient
    {
        // Main palette
        public const string Primary = "#F4A261";      // Warm Zone
        public const string Secondary = "#1B4965";    // Cool Zone
        public const string Accent = "#E76F51";       // Hot Zone
        public const string Background = "#F1FAEE";   // Neutral

        // Status colors
        public const string Heating = "#F07949";      // Rising Heat (distinct orange-red, clearly heating)
        public const string Cooling = "#2C5F7D";      // Falling Cool (lighter, distinct blue, clearly cooling)
        public const string OnTemp = "#2A9D8F";       // Balanced Teal (success, perfect temp)
        public const string Warning = "#F9C74F";      // Caution Yellow (distinct, needs attention)
        public const string Error = "#9B2226";        // Critical Red (danger, system failure)
    }

    public class FarmhouseModern
    {
        // Main palette
        public const string Primary = "#6B705C";      // Sage Smoke
        public const string Secondary = "#CB997E";    // Clay
        public const string Accent = "#DDBEA9";       // Butcher Paper
        public const string Background = "#FFE8D6";   // Warm Neutral

        // Status colors
        public const string Heating = "#D4845F";      // Warm Terracotta (warmer, more energetic clay tone)
        public const string Cooling = "#8B9A8F";      // Cool Sage Gray (lighter, bluer sage, clearly cooler)
        public const string OnTemp = "#A5A58D";       // Soft Balanced Green (success, stable)
        public const string Warning = "#B56576";      // Stall Pink (caution, temperature stall)
        public const string Error = "#9D0208";        // Deep Red (danger, critical issue)
    }
}
