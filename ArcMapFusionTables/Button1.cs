using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ArcMapFusionTables
{
    public class Button1 : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        public Button1()
        {
        }

        protected override void OnClick()
        {
            formAuthorize frm = new formAuthorize();
            frm.ShowDialog();

            ArcMap.Application.CurrentTool = null;
        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

}
