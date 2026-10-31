using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotServer;

public struct RobotCordStats
{
    public int XCord { get; set; }
    public int YCord { get; set; }

    public RobotCordStats()
    {
        XCord = 0;
        YCord = 0;
    }

    public override string ToString() =>
        $"Robot's position: ({XCord}, {YCord})";
}
