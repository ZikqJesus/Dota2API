using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaAPI.utils
{
    public class Dota2HeroesRequestResult
    {
        public Result result { get; set; }
    }

    public class Result
    {
        public Hero[] heroes { get; set; }
        public int status { get; set; }
        public int count { get; set; }
    }

    public class Hero
    {
        public string name { get; set; }
        public int id { get; set; }
    }
}
