using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMR.Services
{
    public class LangCombService
    {
        public static string Succerss(string lab, string name, bool isUpdate= false)
        {
            if (isUpdate)
            {
                return lab +" :"+ name +" "+Lang.Resources.Update + Lang.Resources.Success;
            }
            return lab + " :" + name + " " + Lang.Resources.Add + Lang.Resources.Success;
        }

    }
}
