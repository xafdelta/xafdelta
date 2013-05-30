using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafDelta.Web
{
    [NonPersistent, IsLocal]
    public class ImportMessages : BaseObject
    {
        public ImportMessages(Session session): base(session)
        {
        }

        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        public FileData File1
        {
            get { return GetPropertyValue<FileData>("File1"); }
            set { SetPropertyValue("File1", value); }
        }

        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        public FileData File2
        {
            get { return GetPropertyValue<FileData>("File2"); }
            set { SetPropertyValue("File2", value); }
        }

        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        public FileData File3
        {
            get { return GetPropertyValue<FileData>("File3"); }
            set { SetPropertyValue("File3", value); }
        }
    }
}
