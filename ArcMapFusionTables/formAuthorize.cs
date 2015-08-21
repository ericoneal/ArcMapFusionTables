using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;

namespace ArcMapFusionTables
{
    public partial class formAuthorize : Form
    {
        private const string clientId = "1003120056405-ofafvc699hjujp0a9952g8vjaludk90j.apps.googleusercontent.com";
        private const string clientSecret = "ILrbaMipFC6PxLIQVKZApv-c";
        private string redirectURI = "urn:ietf:wg:oauth:2.0:oob:auto";
        private string scope = "https://www.googleapis.com/auth/fusiontables";
        //OBannon  (lines and points)
        //private string strTableID = "1ommQFLm8eZKFk_HdRZXL8PIZEWNxIU5uFFcba1Zg";
        //BBG  (polygons)
        private string strTableID = "1Z5H9rWY9A0SHT1hJACrnTJE7BricYkMJ18DHdNQ";

        public formAuthorize()
        {
            InitializeComponent();
        }

        private void formAuthorize_Load(object sender, EventArgs e)
        {
            webBrowser1.Navigate(AuthResponse.GetAutenticationURI(scope, clientId, redirectURI));
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

            try
            {

                string Mytitle = ((WebBrowser)sender).DocumentTitle;
                if (Mytitle.IndexOf("Success code=") > -1)
                {

                    string[] str = Mytitle.Split('=');
                    string authCode = str[1];

                    AuthResponse access = AuthResponse.Exchange(authCode, clientId, clientSecret, redirectURI);
                    var o = access;
                    //Refresh token never expires.  Use to refresh access token with each transaction.
                    access.refresh();

                    GetFeatures(access);


                }
            }

            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();

                dynamic obj = JsonConvert.DeserializeObject(resp);
                var messageFromServer = obj;

            }

        }



        private void GetFeatures(AuthResponse access)
        {
            string jsonFusion = String.Format("https://www.googleapis.com/fusiontables/v2/query?sql=SELECT * FROM {0}&access_token={1}", strTableID, access.Access_token);
            try
            {
                WebClient client = new WebClient();
                string result = client.DownloadString(jsonFusion);
    
                FusionTable ft = JsonConvert.DeserializeObject<FusionTable>(result);

                Plot(ft);


                this.Close();
            }
            catch (Exception ex)
            {

            }
        }



        private void Plot(FusionTable ft)
        {
            List<RowPoint> lstGeomsPoint = new List<RowPoint>();
            List<RowLine> lstGeomsLine = new List<RowLine>();
            List<RowPolygon> lstGeomsPolygon = new List<RowPolygon>();


            for (int i = 0; i <= ft.rows.Count - 1; i++)
            {
              
                    List<object> lstRows = ft.rows[i];
                    if (lstRows[2].ToString().ToUpper().Contains("POINT"))
                    {
                        RowPoint geom = JsonConvert.DeserializeObject<RowPoint>(lstRows[2].ToString());
                        lstGeomsPoint.Add(geom);
                    }
                    if (lstRows[2].ToString().ToUpper().Contains("LINE"))
                    {
                        RowLine geom = JsonConvert.DeserializeObject<RowLine>(lstRows[2].ToString());
                        lstGeomsLine.Add(geom);
                    }
                    if (lstRows[2].ToString().ToUpper().Contains("POLYGON"))
                    {
                        RowPolygon geom = JsonConvert.DeserializeObject<RowPolygon>(lstRows[2].ToString());
                        lstGeomsPolygon.Add(geom);
                    }
            }


            if (lstGeomsPoint.Count > 0)
            {
                IFeatureLayer lyrPoints = MakePointFeatureLayer(ft, lstGeomsPoint);
                ArcMap.Document.FocusMap.AddLayer(lyrPoints);
            }
            if (lstGeomsLine.Count > 0)
            {
                IFeatureLayer lyrLines = MakeLineFeatureLayer(ft, lstGeomsLine);
                ArcMap.Document.FocusMap.AddLayer(lyrLines);
            }
            if (lstGeomsPolygon.Count > 0)
            {
                IFeatureLayer lyrPolygons = MakePolygonFeatureLayer(ft, lstGeomsPolygon);
                ArcMap.Document.FocusMap.AddLayer(lyrPolygons);
            }
      


          

        }


        private IFeatureLayer MakePointFeatureLayer(FusionTable ft, List<RowPoint> lstGeomsPoint)
        {
            IFeatureLayer pPointLayer = new FeatureLayerClass();
            pPointLayer.FeatureClass = MakeInMemoryFeatureClass(ft, esriGeometryType.esriGeometryPoint);
            //Set up the Outpoints cursor
            IFeatureCursor pFCurOutPoints = pPointLayer.Search(null, false);
            pFCurOutPoints = pPointLayer.FeatureClass.Insert(true);

            IFeatureBuffer pFBuffer = pPointLayer.FeatureClass.CreateFeatureBuffer();


            IPoint ppoint;
            foreach (RowPoint pt in lstGeomsPoint)
            {

                ppoint = new PointClass();
                ppoint.X = pt.geometry.coordinates[0];
                ppoint.Y = pt.geometry.coordinates[1];


                pFBuffer.Shape = ppoint;


                //for (int i = 0; i <= pFBuffer.Fields.FieldCount - 1; i++)
                //{
                //    string g = pFBuffer.Fields.get_Field(i).Name;
                //    foreach (string col in ft.columns)
                //    {
                        
                //        if (col == pFBuffer.Fields.get_Field(i).Name)
                //        {
                //            pFBuffer.set_Value(pFBuffer.Fields.FindField(col), ft.rows[i]);
                //        }
                      
                //    }
                //}


             
                pFCurOutPoints.InsertFeature(pFBuffer);

            }

           


            pPointLayer.Name = "FusionTablePoint";
            return pPointLayer;


        }

        private IFeatureLayer MakeLineFeatureLayer(FusionTable ft, List<RowLine> lstGeomsLine)
        {
            IFeatureLayer pLineLayer = new FeatureLayerClass();
            pLineLayer.FeatureClass = MakeInMemoryFeatureClass(ft, esriGeometryType.esriGeometryPolyline);
            //Set up the Outpoints cursor
            IFeatureCursor pFCurOutLine = pLineLayer.Search(null, false);
            pFCurOutLine = pLineLayer.FeatureClass.Insert(true);

            IFeatureBuffer pFBuffer = pLineLayer.FeatureClass.CreateFeatureBuffer();


        
            IPointCollection pNewLinePointColl = null;
            IPoint ppoint;
            foreach (RowLine line in lstGeomsLine)
            {
                pNewLinePointColl = new PolylineClass();
                foreach (List<double> pt in line.geometry.coordinates)
                {
                    ppoint = new PointClass();
                   
                    ppoint.X = pt[0];
                    ppoint.Y = pt[1];
                    pNewLinePointColl.AddPoint(ppoint);
                }

                pFBuffer.Shape = pNewLinePointColl as PolylineClass;

                pFCurOutLine.InsertFeature(pFBuffer);


          

            }


            pLineLayer.Name = "FusionTableLine";
            return pLineLayer;
        }

        private IFeatureLayer MakePolygonFeatureLayer(FusionTable ft, List<RowPolygon> lstGeomsPolygon)
        {
            IFeatureLayer pPolyLayer = new FeatureLayerClass();
            pPolyLayer.FeatureClass = MakeInMemoryFeatureClass(ft, esriGeometryType.esriGeometryPolygon);
            //Set up the Outpoints cursor
            IFeatureCursor pFCurOutLine = pPolyLayer.Search(null, false);
            pFCurOutLine = pPolyLayer.FeatureClass.Insert(true);

            IFeatureBuffer pFBuffer = pPolyLayer.FeatureClass.CreateFeatureBuffer();



            IPointCollection pointCollection = null;
            IPoint ppoint;
            foreach (RowPolygon poly in lstGeomsPolygon)
            {
                pointCollection = new PolygonClass();
                foreach (List<double> pt in poly.geometry.coordinates[0])
                {
                    ppoint = new PointClass();
                    ppoint.X = pt[0];
                    ppoint.Y = pt[1];
                    pointCollection.AddPoint(ppoint);
                }

                pFBuffer.Shape = pointCollection as PolygonClass;

                pFCurOutLine.InsertFeature(pFBuffer);




            }


            pPolyLayer.Name = "FusionTablePolygon";
            return pPolyLayer;
        }

        private IFeatureClass MakeInMemoryFeatureClass(FusionTable ft, esriGeometryType geomType)
        {

            try
            {

                ISpatialReferenceFactory pSpatialRefFactory = new SpatialReferenceEnvironmentClass();
                IGeographicCoordinateSystem pGeographicCoordSys = pSpatialRefFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                ISpatialReference pSpaRef = pGeographicCoordSys;
                pSpaRef.SetDomain(-180, 180, -90, 90);

                // Create an in-memory workspace factory.
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.InMemoryWorkspaceFactory");
                IWorkspaceFactory workspaceFactory = Activator.CreateInstance(factoryType) as IWorkspaceFactory;

                // Create an in-memory workspace.
                IWorkspaceName workspaceName = workspaceFactory.Create("", "FusionTable", null, 0);

                // Cast for IName and open a reference to the in-memory workspace through the name object.
                IName name = workspaceName as IName;
                IWorkspace pWorkspace = name.Open() as IWorkspace;



                IFeatureWorkspace workspace = pWorkspace as IFeatureWorkspace;
                UID CLSID = new UID();
                CLSID.Value = "esriGeodatabase.Feature";

                IFields pFields = new FieldsClass();
                IFieldsEdit pFieldsEdit = pFields as IFieldsEdit;
                pFieldsEdit.FieldCount_2 = ft.columns.Count + 1;



                IGeometryDef pGeomDef = new GeometryDef();
                IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
                pGeomDefEdit.GeometryType_2 = geomType;
                pGeomDefEdit.SpatialReference_2 = pSpaRef;



                IField pField;
                IFieldEdit pFieldEdit;

               

                pField = new FieldClass();
                pFieldEdit = pField as IFieldEdit;
                pFieldEdit.AliasName_2 = "SHAPE";
                pFieldEdit.Name_2 = "SHAPE";
                pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                pFieldEdit.GeometryDef_2 = pGeomDef;
                pFieldsEdit.set_Field(0, pFieldEdit);

                int k = 1;
                int i = 0;
                foreach(string col in ft.columns)
                {
                    pField = new FieldClass();
                    pFieldEdit = pField as IFieldEdit;
                    pFieldEdit.AliasName_2 = ft.columns[i];
                    pFieldEdit.Name_2 = ft.columns[i];
                    pFieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEdit.set_Field(k, pFieldEdit);
                    k++;
                    i++;
                }




                string strFCName = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName());
                char[] chars = strFCName.ToCharArray();
                if (Char.IsDigit(chars[0]))
                {
                    strFCName = strFCName.Remove(0, 1);
                }
                

                IFeatureClass pFeatureClass = workspace.CreateFeatureClass(strFCName, pFieldsEdit, CLSID, null, esriFeatureType.esriFTSimple, "SHAPE", "");
                return pFeatureClass;


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show(ex.StackTrace);
                return null;
            }

        }











    }


    public class FusionTable
    {
        public string kind { get; set; }
        public List<string> columns { get; set; }
        public List<List<object>> rows { get; set; }
    }

    public class RowPoint
    {
        public GeometryPoint geometry { get; set; }
    }

    public class RowLine
    {
        public GeometryLine geometry { get; set; }
    }

    public class RowPolygon
    {
        public GeometryPolygon geometry { get; set; }
    }


    public class GeometryPoint
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    public class GeometryLine
    {
        public string type { get; set; }
        public List<List<double>> coordinates { get; set; }
    }

    public class GeometryPolygon
    {
        public string type { get; set; }
        public List<List<List<double>>> coordinates { get; set; }
    }

   
    

    
}
