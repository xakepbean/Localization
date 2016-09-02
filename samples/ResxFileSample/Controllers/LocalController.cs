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
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Resources;
using System.Collections;

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

        public List<TreeNode> ResxFile(string ID)
        {
            var locOptions = HttpContext.RequestServices.GetService<IOptions<LocalizationOptions>>();
            string ResourcesPath = locOptions.Value.ResourcesPath.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)[0];
            var razorOptions = HttpContext.RequestServices.GetService<IOptions<RazorViewEngineOptions>>();

            var razorPath = razorOptions.Value.AreaViewLocationFormats.Select(w =>
            {
                var vpath = w.TrimStart('/').Split('/');
                if (vpath.Length > 0)
                    return vpath[0];
                return string.Empty;
            }).Where(w => !string.IsNullOrWhiteSpace(w)).Distinct().Concat(
             razorOptions.Value.ViewLocationFormats.Select(w =>
            {
                var vpath = w.TrimStart('/').Split('/');
                if (vpath.Length > 0)
                    return vpath[0];
                return string.Empty;
            }).Where(w => !string.IsNullOrWhiteSpace(w)).Distinct()).Distinct();

            List<TreeNode> ReturnTree = new List<TreeNode>();
            if (string.IsNullOrWhiteSpace(ID))
            {
                ReturnTree.Add(new TreeNode() { id = ResourcesPath, name = ResourcesPath, isParent = true });
                //ReturnTree.AddRange(razorPath.Where(w => _env.ContentRootFileProvider.GetDirectoryContents(w).Exists).Select(w => new TreeNode() { id = w, name = w, isParent = true }));
            }
            else
            {
                var vRoorDir= ID.Split('.')[0];
                if (razorPath.Contains(vRoorDir))
                {
                    ID = ID.Replace('.', '/');
                    var vDir = _env.ContentRootFileProvider.GetDirectoryContents(ID);
                    if (vDir.Exists)
                    {
                        var vFile = vDir.GetEnumerator();
                        while (vFile.MoveNext())
                        {
                            ReturnTree.Add(new TreeNode()
                            {
                                id = vFile.Current.PhysicalPath.Substring(_env.ContentRootPath.Length+1).Replace(Path.DirectorySeparatorChar,'.'),
                                name = vFile.Current.Name,
                                isParent = vFile.Current.IsDirectory
                            });
                        }
                    }
                }
                else
                {
                    ID += ".";
                    var vResource = Assembly.GetEntryAssembly().GetManifestResourceNames().Select(w => w.Substring(w.IndexOf('.') + 1)).Where(w => w.StartsWith(ID));
                    ReturnTree.AddRange(vResource.Select(w =>
                    {
                        var vID = w.Substring(0, w.IndexOf('.', ID.Length));
                        var vName = vID.Substring(ID.Length);
                        var vLast = w.Substring(vID.Length);
                        vName += vLast.Length > 10 ? "" : ".resx";
                        return new TreeNode()
                        {
                            id = vID,
                            name = vName,
                            isParent = vLast.Length > 10
                        };
                    }).Distinct());
                }
            }
            return ReturnTree;
        }

        public IActionResult Edit(string ID)
        {
            return View(ResxFile(null));
        }
        
        public IActionResult EditResx(string ID, string ReName)
        {
            if (string.IsNullOrWhiteSpace(ID) || string.IsNullOrWhiteSpace(ReName))
            {
                return View(new List<ResouresEditModel>());
            }

            ViewData["ReName"] = ReName;
            if (ReName.EndsWith(".cshtml",StringComparison.OrdinalIgnoreCase))
            {
                var locOptions = HttpContext.RequestServices.GetService<IOptions<LocalizationOptions>>();
                string ResourcesPath = Path.Combine(_env.ContentRootPath, locOptions.Value.ResourcesPath);

                string MapPath = Path.Combine(ResourcesPath, ReName.Replace('.', Path.DirectorySeparatorChar) + ".resx");
                string NewMapPath = Path.Combine(ResourcesPath, ReName.Replace('.', Path.DirectorySeparatorChar) + "." + ID + ".resx");
                return View(new List<ResouresEditModel>()); //return View(LoadResource(MapPath, NewMapPath));
            }
            else
            {               
                return View(LoadResource(ID, ReName));
            }

        }
      
        [HttpPost]
        public IActionResult EditResx(string ID, string ReName, List<ResouresEditModel> RMode)
        {
            if (string.IsNullOrWhiteSpace(ID) || string.IsNullOrWhiteSpace(ReName))
            {
                return View(RMode);
            }
            ViewData["ReName"] = ReName;
            SaveResource(ID, ReName, RMode);
            return View(RMode);
        }

        private List<ResouresEditModel> LoadResource(string CultureName, string ResourceName)
        {
            List<ResouresEditModel> ListRes = new List<ResouresEditModel>();
            #region Source Resource
            using (var resourceStream = Assembly.GetEntryAssembly().GetManifestResourceStream(_env.ApplicationName + "." + ResourceName + ".resources"))
            {
                if (resourceStream != null)
                {
                    using (var resources = new ResourceReader(resourceStream))
                    {
                        foreach (DictionaryEntry entry in resources)
                        {
                            ListRes.Add(new ResouresEditModel
                            {
                                ResourceName = (string)entry.Key,
                                DefaultValue = (string)entry.Value
                            });
                        }
                    }
                }
            }
            #endregion
            var vResxFile = _env.ContentRootFileProvider.GetFileInfo(ResourceName + "." + CultureName + ".resx");
            if (vResxFile.Exists)
            {
                Dictionary<string, string> DicNew = new Dictionary<string, string>();
                using (var FStream = vResxFile.CreateReadStream())
                {
                    var d = XElement.Load(FStream);
                    foreach (var node in d.Elements("data"))
                    {
                        if (node.Attribute("name") != null && node.Element("value") != null)
                        {
                            string name = node.Attribute("name").Value;
                            string val = node.Element("value").Value;
                            if (!string.IsNullOrWhiteSpace(val) && !DicNew.ContainsKey(name))
                                DicNew.Add(name, val);
                        }
                    }
                    FStream.Dispose();
                }
                for (int i = 0; i < ListRes.Count; i++)
                {
                    if (DicNew.ContainsKey(ListRes[i].ResourceName))
                        ListRes[i].LocalizedValue = DicNew[ListRes[i].ResourceName];
                }
            }
            return ListRes;
        }

        private List<ResouresEditModel> SaveResource(string CultureName, string ResourceName, List<ResouresEditModel> RMode)
        {
            string NewMapPath = Path.Combine(_env.ContentRootPath, ResourceName.Replace('.', Path.DirectorySeparatorChar) + "." + CultureName + ".resx");
            var resDoc = new XElement("Init");
            if (!System.IO.File.Exists(NewMapPath))
            {
                resDoc = XElement.Parse("<?xml version=\"1.0\" encoding=\"utf-8\"?><root><resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader><resheader name=\"version\"><value>2.0</value></resheader><resheader name=\"reader\"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader><resheader name=\"writer\"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader></root>");
            }
            else
            {
                resDoc = XElement.Load(NewMapPath);
            }

            var changedResources = new Dictionary<string, string>();
            foreach (var di in RMode)
            {
                if (di.DefaultValue != di.LocalizedValue)
                {
                    var node = resDoc.Elements().Where(w => w.Name == "data" && w.Attribute("name") != null && w.Attribute("name").Value == di.ResourceName);
                    if (node.Count() == 0)
                    {
                        resDoc.Add(new XElement("data", new XAttribute("name", di.ResourceName), new XElement("value", di.LocalizedValue)));
                    }
                    else
                    {
                        node.First().Element("value").Value = di.LocalizedValue;
                    }
                }
            }
            if (resDoc.Elements("data").Count() > 0)
            {
                System.IO.MemoryStream NewFileStream = new MemoryStream();
                resDoc.Save(NewFileStream);
                System.IO.File.WriteAllBytes(NewMapPath, NewFileStream.ToArray());
                NewFileStream.Dispose();
            }
            return RMode;
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
