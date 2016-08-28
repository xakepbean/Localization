using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;
using System.IO;
using ResxFileSample.Models;
using System.Xml;
using System.Xml.Linq;
using System.Text.Encodings.Web;

namespace ResxFileSample.Controllers
{
    public class LocalController:Controller
    {
        private IHostingEnvironment _env = null;
        public LocalController(IHostingEnvironment env)
        {
            _env = env;
        }

        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Add(List<Person> persons, List<string> movies)
        {
            return View();
        }

        public IActionResult Index() {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            RequestCultureProvider AcceptUI = requestCulture.Provider as RequestCultureProvider;
            return View(AcceptUI.Options.SupportedCultures);
        }

        
        public IActionResult Edit(string ID)
        {
            var locOptions =HttpContext.RequestServices.GetService<IOptions<LocalizationOptions>>();
            string ResourcesPath = Path.Combine(_env.ContentRootPath, locOptions.Value.ResourcesPath);
            //var vNode = GetResourcesFile(MapPath);
            //return View(vNode);
            string MapPath= Path.Combine(ResourcesPath,ID.Replace('.',Path.DirectorySeparatorChar)+".resx");
            string NewMapPath = Path.Combine(ResourcesPath, ID.Replace('.', Path.DirectorySeparatorChar) + "."+ CultureInfo.CurrentCulture.Name + ".resx");
            return View(LoadResource(MapPath, NewMapPath));

        }
        private List<ResouresEditModel> LoadResource(string filepath, string newfilepath)
        {
            var ht = new Dictionary<string, ResouresEditModel>();
            var d = XElement.Load(filepath);
            foreach (var node in d.Elements("data"))
            {
                if (node.Attribute("name") != null && node.Element("value") !=null)
                {
                    string name = node.Attribute("name").Value;
                    string val = node.Element("value").Value;
                    if (!string.IsNullOrWhiteSpace(val) && !ht.ContainsKey(name))
                        ht.Add(name, new ResouresEditModel() { Name = name, OldValue = val, NewValue = val });
                }
            }

            return ht.Select(w => w.Value).ToList();
        }

        private List<ResouresEditModel> SaveResource(string filepath, string newfilepath,string PathName,List<ResouresEditModel> RMode)
        {
            var defDoc = XElement.Load(filepath);
            var resDoc = new XElement("Init");
            if (!System.IO.File.Exists(newfilepath))
            {
                resDoc = XElement.Parse("<?xml version=\"1.0\" encoding=\"utf-8\"?><root><resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader><resheader name=\"version\"><value>2.0</value></resheader><resheader name=\"reader\"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader><resheader name=\"writer\"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader></root>");
            }
            else
            {
                resDoc = XElement.Load(newfilepath);
            }
           
            var changedResources = new Dictionary<string, string>();
            foreach (var di in RMode)
            {
                var resourceKey = di.Name;
                var txtValue = di.NewValue;
                var txtDefault = di.OldValue;
                if (txtDefault != txtValue)
                {
                    var node = resDoc.Elements().Where(w => w.Name == "data" && w.Attribute("name") != null && w.Attribute("name").Value == di.Name);
                    if (node.Count() == 0)
                    {
                      resDoc.Add(new XElement("data", new XAttribute("name", di.Name),  new XElement("value", di.NewValue)));
                    }
                    else
                    {
                        node.First().Element("value").Value = di.NewValue;
                    }
                }
            }
            if (resDoc.Elements("data").Count() > 0)
            {
                System.IO.MemoryStream NewFileStream = new MemoryStream();
                resDoc.Save(NewFileStream);
                System.IO.File.WriteAllBytes(newfilepath, NewFileStream.ToArray());
                NewFileStream.Dispose();
            }

            return RMode;
        }

        [HttpPost]
        public IActionResult Edit(string ID, List<ResouresEditModel> RMode)
        {
            var locOptions = HttpContext.RequestServices.GetService<IOptions<LocalizationOptions>>();
            string ResourcesPath = Path.Combine(_env.ContentRootPath, locOptions.Value.ResourcesPath);
            //var vNode = GetResourcesFile(MapPath);
            //return View(vNode);
            string MapPath = Path.Combine(ResourcesPath, ID.Replace('.', Path.DirectorySeparatorChar) + ".resx");
            string NewMapPath = Path.Combine(ResourcesPath, ID.Replace('.', Path.DirectorySeparatorChar) + "."+ CultureInfo.CurrentUICulture.Name + ".resx");

            return View(SaveResource(MapPath, NewMapPath, ID, RMode));
        }
        //public IActionResult Edit(string ID)
        //{
        //    var locOptions = HttpContext.RequestServices.GetService<IOptions<LocalizationOptions>>();
        //    string MapPath = Path.Combine(_env.ContentRootPath, locOptions.Value.ResourcesPath);
        //    var vNode = GetResourcesFile(MapPath);
        //    return View(vNode);
        //}

        private List<TreeNode> GetResourcesFile(string MapPath)
        {
            List<TreeNode> TNode = new List<TreeNode>();
            foreach (var item in Directory.GetDirectories(MapPath))
            {
                TNode.Add(new TreeNode() { name = new DirectoryInfo(item).Name, children = GetResourcesFile(item) });
            }
            foreach (var item in Directory.GetFiles(MapPath))
            {
                TNode.Add(new TreeNode() { name = Path.GetFileName(item) });
            }
            return TNode;
        }

        public IActionResult Create() => View();

        public IActionResult CreateLang(string ID)
        {
            if (!string.IsNullOrWhiteSpace(ID))
            {
                var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
                RequestCultureProvider AcceptUI = requestCulture.Provider as RequestCultureProvider;
                CultureInfo CInfo = new CultureInfo(ID);
                if (AcceptUI.Options.SupportedCultures.Where(w => w.Name == ID).Count() == 0)
                {
                    AcceptUI.Options.SupportedUICultures.Add(CInfo);
                    AcceptUI.Options.SupportedCultures.Add(CInfo);
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult Redirect() => RedirectToAction("Index");
    }
}
