using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace XafDelta.Win
{
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion) { }
    }
}
