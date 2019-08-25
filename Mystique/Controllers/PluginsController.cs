﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Mystique.Core;
using Mystique.Core.Contracts;
using Mystique.Core.DomainModel;
using Mystique.Core.Mvc.Extensions;
using Mystique.Mvc.Infrastructure;
using System;
using System.IO;
using System.Linq;

namespace Mystique.Controllers
{
    public class PluginsController : Controller
    {
        private IPluginManager _pluginManager = null;
        private ApplicationPartManager _partManager = null;

        public PluginsController(IPluginManager pluginManager, ApplicationPartManager partManager)
        {
            _pluginManager = pluginManager;
            _partManager = partManager;
        }

        private void RefreshControllerAction()
        {
            MystiqueActionDescriptorChangeProvider.Instance.HasChanged = true;
            MystiqueActionDescriptorChangeProvider.Instance.TokenSource.Cancel();
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View(_pluginManager.GetAllPlugins());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload()
        {
            var package = new PluginPackage(Request.GetPluginStream());
            _pluginManager.AddPlugins(package);

            return RedirectToAction("Index");
        }

        public IActionResult Enable(Guid id)
        {
            _pluginManager.EnablePlugin(id);

            return RedirectToAction("Index");
        }

        public IActionResult Disable(Guid id)
        {
            var module = _pluginManager.GetPlugin(id);
            _pluginManager.DisablePlugin(id);
            var moduleName = module.Name;

            var last = _partManager.ApplicationParts.First(p => p.Name == moduleName);
            _partManager.ApplicationParts.Remove(last);

            RefreshControllerAction();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(Guid id)
        {
            var module = _pluginManager.GetPlugin(id);
            _pluginManager.DisablePlugin(id);
            _pluginManager.DeletePlugin(id);
            var moduleName = module.Name;

            var matchedItem = _partManager.ApplicationParts.FirstOrDefault(p => p.Name == moduleName);

            if (matchedItem != null)
            {
                _partManager.ApplicationParts.Remove(matchedItem);
                matchedItem = null;
            }

            RefreshControllerAction();

            PluginsLoadContexts.RemovePluginContext(module.Name);

            var directory = new DirectoryInfo($"{AppDomain.CurrentDomain.BaseDirectory}Modules/{module.Name}");
            directory.Delete(true);

            return RedirectToAction("Index");
        }
    }
}