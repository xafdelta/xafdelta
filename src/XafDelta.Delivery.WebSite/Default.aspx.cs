using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class _Default : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var filesPath = Context.Server.MapPath(@"/Files");
        BulletedList1.Items.Clear();
        if (Request.Files.Count > 0)
        {
            foreach (var fileKey in Request.Files.AllKeys)
            {
                var file = Request.Files[fileKey];
                file.SaveAs(Path.Combine(filesPath, file.FileName));
            }
        }
        else if(Request.Params["DownloadFile"] != null)
        {
            var fileName = Path.Combine(filesPath, Request.Params["DownloadFile"]);
            if(File.Exists(fileName))
            {
                Response.Clear();
                var fileData = File.ReadAllBytes(fileName);
                Response.OutputStream.Write(fileData, 0, fileData.Length);
                Response.OutputStream.Close();
                Response.Flush();
                Response.End();
            }
        }
        else if (Request.Params["DeleteFile"] != null)
            {
                var fileName = Path.Combine(filesPath, Request.Params["DeleteFile"]);
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    BulletedList1.Items.Add(string.Format("File {0} deleted", fileName));
                }
            }
        else
        {
            (new DirectoryInfo(filesPath)).GetFiles().ToList().ForEach(x => BulletedList1.Items.Add(x.Name));
        }
    }
}
