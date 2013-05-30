using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.Security;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using XafDelta.Messaging;
using XafDelta.Replication;

namespace XafDelta
{
    // ReSharper disable InconsistentNaming

    /// <summary>
    /// Selector mode
    /// </summary>
    public enum SelectorMode
    {
        /// <summary>
        /// Black list (all model classes and interfaces are involved in replication by default)
        /// </summary>
        BlackList,
        /// <summary>
        /// White list (all model classes and interfaces are excluded from replication by default)
        /// </summary>
        WhiteList
    }

    /// <summary>
    /// Recipient selector type
    /// </summary>
    public enum SelectorType
    {
        /// <summary>
        /// Include nodes
        /// </summary>
        Include,

        /// <summary>
        /// Exclude nodes
        /// </summary>
        Exclude
    }

    /// <summary>
    /// I selector node
    /// </summary>
    public interface ISelectorNode : IModelNode
    {
        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        /// <value>
        /// The expression.
        /// </value>
        [CriteriaObjectTypeMember("ContextType")]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafApplication.CurrentVersion, typeof(System.Drawing.Design.UITypeEditor))]
        string Expression { get; set; }

        /// <summary>
        /// Gets the type of the context.
        /// </summary>
        /// <value>
        /// The type of the context.
        /// </value>
        [Browsable(false)]
        Type ContextType { get; }

        /// <summary>
        /// Gets or sets the type of the selector.
        /// </summary>
        /// <value>
        /// The type of the selector.
        /// </value>
        [Required]
        SelectorType SelectorType { get; set; }

        /// <summary>
        /// Gets the model class.
        /// </summary>
        [Category("Data")]
        [Browsable(false)]
        IModelClass ModelClass { get; }
    }

    /// <summary>
    /// Selectors nodes generator base class
    /// </summary>
    public abstract class SelectorsNodesGeneratorBase : ModelNodesGeneratorBase
    {
        /// <summary>
        /// Generate default selectors
        /// </summary>
        /// <param name="node">The node.</param>
        protected override void GenerateNodesCore(ModelNode node)
        {
            if (node == null || node.Parent == null || !(node.Parent is IModelClass)) return;

            var modelClass = node.Parent as IModelClass;
            var classType = modelClass.TypeInfo.Type;

            if (modelClass.TypeInfo.Implements<ISimpleUser>()
                || modelClass.TypeInfo.Implements<IUser>()
                || modelClass.TypeInfo.Implements<IRole>()
                || typeof(AuditDataItemPersistent).IsAssignableFrom(classType)
                || typeof(XPWeakReference).IsAssignableFrom(classType)
                || typeof(ModuleInfo).IsAssignableFrom(classType)
                || typeof(PersistentPermission).IsAssignableFrom(classType)
                /* 11.2.7 */
                || typeof(PermissionsContainer).IsAssignableFrom(classType)
                || typeof(SecurityProxyBase).IsAssignableFrom(classType)
                || modelClass.TypeInfo.Implements<ISecurityRole>()
                || modelClass.TypeInfo.Implements<ISecurityUser>()
                )
            {
                var emptySelector = AddSelectorNode(node);
                emptySelector.SelectorType = SelectorType.Exclude;
                emptySelector.SetValue("Id", "Disabled by XafDelta for " + modelClass.ShortName);
            }

            foreach (var selectorAttribute in FindModelClassSelectorAttributes(modelClass))
            {
                var newSelector = AddSelectorNode(node);
                newSelector.Expression = selectorAttribute.Expression;
                newSelector.SelectorType = selectorAttribute.SelectorType;
                if(!string.IsNullOrEmpty(selectorAttribute.Name))
                    newSelector.SetValue("Id", selectorAttribute.Name);
            }

            var attr = FindNoneAttribute(modelClass) ?? modelClass.TypeInfo.FindAttribute<IsLocalAttribute>();

            if (attr != null)
            {
                var newSelector = AddSelectorNode(node);
                newSelector.Expression = "";
                newSelector.SetValue("Id", attr.GetType().Name + " for " + modelClass.Name);
                newSelector.SelectorType = SelectorType.Exclude;
            }
        }

        /// <summary>
        /// Finds the model class selector attributes.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns></returns>
        protected abstract IEnumerable<SelectorAttribute> FindModelClassSelectorAttributes(IModelClass modelClass);
        /// <summary>
        /// Adds the selector node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        protected abstract ISelectorNode AddSelectorNode(IModelNode node);
        /// <summary>
        /// Finds the none attribute.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns></returns>
        protected abstract Attribute FindNoneAttribute(IModelClass modelClass);
    }

    #region Recipient selectors

    /// <summary>
    /// Replication recipient selector node
    /// </summary>
    [Description("Specifies criteria for replication routing")]
    public interface IRecipientSelectorNode : ISelectorNode
    {
    }

    /// <summary>
    /// Replication recipient selector node logic
    /// </summary>
    [DomainLogic(typeof (IRecipientSelectorNode))]
    public class RecipientSelectorNodeLogic
    {
        /// <summary>
        /// Get_s the type of the context.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Context type</returns>
        public static Type Get_ContextType(IRecipientSelectorNode node)
        {
            Type result = null;
            if (node != null && node.ModelClass != null)
            {
                /* 11.2.7 */
                var replModelNode = (IModelReplicationNode)node.Application.GetNode("Replication");
                if (replModelNode != null)
                {
                    result = typeof (RecipientsContext<>).MakeGenericType(node.ModelClass.TypeInfo.Type);
                }
            }
            return result;
        }

        /// <summary>
        /// Get the model class.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Model class</returns>
        public static IModelClass Get_ModelClass(IRecipientSelectorNode node)
        {
            return node.Parent.Parent as IModelClass;
        }
    }

    /// <summary>
    /// Selectors nodes container nodes
    /// </summary>
    [ImageName("XafDelta")]
    [ModelNodesGenerator(typeof (RecipientSelectorsNodesGenerator))]
    public interface IRecipientSelectorNodeList : IModelNode, IModelList<IRecipientSelectorNode>
    {
    }

    /// <summary>
    /// Selectors nodes generator
    /// </summary>
    public class RecipientSelectorsNodesGenerator : SelectorsNodesGeneratorBase
    {
        /// <summary>
        /// Append empty selector to BaseObject (replicate all BaseObject descendants by default)
        /// </summary>
        /// <param name="node">The node.</param>
        protected override void GenerateNodesCore(ModelNode node)
        {
            base.GenerateNodesCore(node);

            if (node == null || node.Parent == null || !(node.Parent is IModelClass)) return;
            var modelClass = node.Parent as IModelClass;

            foreach (var member in modelClass.OwnMembers)
            {
                var memberAttr = member.MemberInfo.FindAttribute<NonReplicableAttribute>()
                         ?? (Attribute)member.MemberInfo.FindAttribute<IsLocalAttribute>();

                if (memberAttr != null)
                {
                    var newSelector = node.AddNode<IRecipientSelectorNode>();
                    newSelector.SetValue("Id", memberAttr.GetType().Name + " for " + member.Name + " property");
                    newSelector.Expression = string.Format(@"Not IsNull(ProtocolRecord) And ProtocolRecord.PropertyName = '{0}'", member.Name);
                    newSelector.SelectorType = SelectorType.Exclude;
                }
            }
        }

        /// <summary>
        /// Finds the model class selector attributes.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns></returns>
        protected override IEnumerable<SelectorAttribute> FindModelClassSelectorAttributes(IModelClass modelClass)
        {
            return modelClass.TypeInfo.FindAttributes<RecipientSelectorAttribute>().Cast<SelectorAttribute>();
        }

        /// <summary>
        /// Adds the selector node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        protected override ISelectorNode AddSelectorNode(IModelNode node)
        {
            return node.AddNode<IRecipientSelectorNode>();
        }

        /// <summary>
        /// Finds the none attribute.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns></returns>
        protected override Attribute FindNoneAttribute(IModelClass modelClass)
        {
            return modelClass.TypeInfo.FindAttribute<NonReplicableAttribute>();
        }
    }

    #endregion

    #region Snapshot

    /// <summary>
    /// Replication snapshot node
    /// </summary>
    [Description("Specifies criteria for snapshot building")]
    public interface ISnapshotSelectorNode : ISelectorNode
    {
    }

    /// <summary>
    /// Replication snapshot node logic
    /// </summary>
    [DomainLogic(typeof(ISnapshotSelectorNode))]
    public class SnapshotSelectorNodeLogic
    {
        /// <summary>
        /// Get_s the type of the context.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Context type</returns>
        public static Type Get_ContextType(ISnapshotSelectorNode node)
        {
            Type result = null;
            if (node != null && node.ModelClass != null)
            {
                result = node.ModelClass.TypeInfo.Type;
            }
            return result;
        }

        /// <summary>
        /// Get the model class.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Model class</returns>
        public static IModelClass Get_ModelClass(ISnapshotSelectorNode node)
        {
            return node.Parent.Parent as IModelClass;
        }
    }

    /// <summary>
    /// Snapshots nodes container nodes
    /// </summary>
    [ImageName("XafDelta")]
    [ModelNodesGenerator(typeof(SnapshotsNodesGenerator))]
    public interface ISnapshotSelectorNodeList : IModelNode, IModelList<ISnapshotSelectorNode>
    {
    }

    /// <summary>
    /// Snapshots nodes generator
    /// </summary>
    public class SnapshotsNodesGenerator : SelectorsNodesGeneratorBase
    {
        /// <summary>
        /// Finds the model class selector attributes.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns></returns>
        protected override IEnumerable<SelectorAttribute> FindModelClassSelectorAttributes(IModelClass modelClass)
        {
            return modelClass.TypeInfo.FindAttributes<SnapshotSelectorAttribute>().Cast<SelectorAttribute>();
        }

        /// <summary>
        /// Adds the selector node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        protected override ISelectorNode AddSelectorNode(IModelNode node)
        {
            return node.AddNode<ISnapshotSelectorNode>();
        }

        /// <summary>
        /// Finds the none attribute.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns></returns>
        protected override Attribute FindNoneAttribute(IModelClass modelClass)
        {
            return modelClass.TypeInfo.FindAttribute<NonSnapshotAttribute>();
        }
    }

    /// <summary>
    /// Non snapshot member node
    /// </summary>
    public interface INonSnapshotMemberNode : IModelNode
    {
        /// <summary>
        /// Gets or sets a value indicating whether use this member in snapshot.
        /// </summary>
        /// <value>
        ///   <c>true</c> if use this member in snapshot; otherwise, <c>false</c>.
        /// </value>
        [Category("Replication")]
        bool NonSnapshot { get; set; }
    }

    /// <summary>
    /// Non snapshot member node logic
    /// </summary>
    [DomainLogic(typeof(INonSnapshotMemberNode))]
    public class NonSnapshotMemberNodeLogic
    {
        /// <summary>
        /// Get_s the non snapshot.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>NonSnapshot attribute of member</returns>
        public static bool Get_NonSnapshot(INonSnapshotMemberNode node)
        {
            var result = false;
            if(node is IModelMember)
            {
                var modelMember = (IModelMember) node;
                var attr = modelMember.MemberInfo.FindAttribute<NonSnapshotAttribute>() ??
                           (Attribute) modelMember.MemberInfo.FindAttribute<IsLocalAttribute>();
                result = attr != null;
            }
            return result;
        }
    }

    #endregion

    /// <summary>
    /// Not for protocol member node
    /// </summary>
    public interface INotForProtocolNode : IModelNode
    {
        /// <summary>
        /// Gets or sets a value indicating whether protocol this item changes.
        /// </summary>
        /// <value>
        ///   <c>true</c> if use this protocol; otherwise, <c>false</c>.
        /// </value>
        [Category("Replication")]
        bool NotForProtocol { get; set; }
    }

    /// <summary>
    /// Not for protocol member node logic
    /// </summary>
    [DomainLogic(typeof(INotForProtocolNode))]
    public class NotForProtocolNodeLogic
    {
        /// <summary>
        /// Get_s the NotForProtocolt.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>NotForProtocol attribute value for node</returns>
        public static bool Get_NotForProtocol(INotForProtocolNode node)
        {
            var result = false;
            if (node is IModelClass)
            {
                var modelClass = (IModelClass)node;
                var attr = modelClass.TypeInfo.FindAttribute<NotForProtocolAttribute>() ??
                           (Attribute)modelClass.TypeInfo.FindAttribute<IsLocalAttribute>();
                result = attr != null || !modelClass.TypeInfo.IsPersistent;
            }
            else
            if (node is IModelMember)
            {
                var modelMember = (IModelMember)node;
                var attr = modelMember.MemberInfo.FindAttribute<NotForProtocolAttribute>() ??
                           (Attribute)modelMember.MemberInfo.FindAttribute<IsLocalAttribute>();
                result = attr != null;
            }
            return result;
        }
    }

    /// <summary>
    /// Model class extensions
    /// </summary>
    public interface IClassSelectorsNode : IModelNode
    {
        /// <summary>
        /// Gets the replication recipient selectors.
        /// </summary>
        [Description("Replication recipient selectors")]
        [Category("Replication")]
        IRecipientSelectorNodeList RecipientSelectors { get; }

        /// <summary>
        /// Gets the replication recipient Snapshots.
        /// </summary>
        [Description("Replication snapshot selectors")]
        [Category("Replication")]
        ISnapshotSelectorNodeList SnapshotSelectors { get; }
    }

    /// <summary>
    /// Model replication node
    /// </summary>
    [ImageName("XafDelta")]
    public interface IModelReplicationNode : IModelNode
    {
        /// <summary>
        /// Specifies type of the crypto algorithm for the transport packet encryption
        /// </summary>
        /// <value>
        /// The type of the crypto algorithm.
        /// </value>
        [Description("Specifies type of the crypto algorithm for the transport packet encryption.")]
        [Category("Data")]
        [DefaultValue(CryptoAlgorithmType.RijndaelManaged)]
        CryptoAlgorithmType CryptoAlgorithmType { get; set; }

        /// <summary>
        /// Specifies password for transport packet encryption (decryption)
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        [Description("Specifies password for encrypt/decrypt.")]
        [Category("Data")]
        [NonPersistentDc]
        [Custom("IsPassword", "True")]
        string Password { get; set; }

        /// <summary>
        /// Gets or sets the protected password.
        /// </summary>
        /// <value>
        /// The protected password.
        /// </value>
        [Browsable(false)]
        string ProtectedPassword { get; set; }

        /// <summary>
        /// Specifies a value indicating whether anonymous packages is granted to load.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if anonymous packages loading is allowed; otherwise, <c>false</c>.
        /// </value>
        [Description("Specifies a value indicating whether anonymous packages is granted to load.")]
        [Category("Data")]
        [DefaultValue(false)]
        bool AnonymousPackagesAllowed { get; set; }

        /// <summary>
        /// Specifies a value indicating whether external protocol records replication is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if replicate external data; otherwise, <c>false</c>.
        /// </value>
        [Description("Specifies a value indicating whether external protocol records replication is enabled.")]
        [Category("Data")]
        [DefaultValue(false)]
        bool ReplicateExternalData { get; set; }

        /// <summary>
        /// Gets or sets the type of the routing.
        /// </summary>
        /// <value>
        /// The type of the routing.
        /// </value>
        [Description("Package routing type.")]
        [Category("Data")]
        [DefaultValue(RoutingType.BroadcastRouting)]
        RoutingType RoutingType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tickets system is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if use tickets; otherwise, <c>false</c>.
        /// </value>
        [Description("Enable replication system tickets system.")]
        [Category("Data")]
        [DefaultValue(false)]
        bool UseTickets { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exported package should be markerd as sent.
        /// </summary>
        /// <value>
        ///   <c>true</c> if export means send; otherwise, <c>false</c>.
        /// </value>
        [Description("Assume package export is send operation."
            + " If True then packages and tickets after export will be marked as sent."
            + " Set this flag to True for manual delivery of packages and tickets.")]
        [Category("Data")]
        [DefaultValue(true)]
        bool MarkExportedAsSent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether XafDelta should create external protocol records 
        /// in target database while playback replication packet.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if create external protocol records; otherwise, <c>false</c>.
        /// </value>
        [Description("Specifies whether XafDelta should create loaded protocol records in current application database. "
            + "Set this flag to True if you like to see all loaded protocol events in objects protocol records. "
            + "Set it to False to decrease protocol data size. "
            + "If RoutingType == RoutingType.TaskRouting then this flag is ignored.")]
        [Category("Data")]
        [DefaultValue(true)]
        bool CreateExternalProtocolRecords { get; set; }

        /// <summary>
        /// Gets or sets the selector mode.
        /// </summary>
        /// <value>
        /// The selector mode.
        /// </value>
        [Category("Data")]
        SelectorMode SelectorMode { get; set; }
    }

    /// <summary>
    /// Routing type
    /// </summary>
    public enum RoutingType
    {
        /// <summary>
        /// BroadcastRouting - put all protocol records into single package (for all recipients)
        /// </summary>
        BroadcastRouting,
        /// <summary>
        /// TaskRouting - create separate package for each of recipient nodes
        /// </summary>
        TaskRouting
    }

    /// <summary>
    /// Model replication node logic
    /// </summary>
    [DomainLogic(typeof(IModelReplicationNode))]
    public class ModelReplicationNodeLogic
    {
        #region Encryption for Password attribute

        private const string BaseString = "#frhn(yr!KNuТв00+*Рв";

        /// <summary>
        /// Get the password.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Password</returns>
// ReSharper disable InconsistentNaming
        public static string Get_Password(IModelReplicationNode node)
// ReSharper restore InconsistentNaming
        {
            return decryptString(node.ProtectedPassword);
        }

        /// <summary>
        /// Set the password.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="value">The value.</param>
// ReSharper disable InconsistentNaming
        public static void Set_Password(IModelReplicationNode node, string value)
// ReSharper restore InconsistentNaming
        {
            node.ProtectedPassword = encryptString(value);
        }

        private static string decryptString(string source)
        {
            var result = source;
            if (!string.IsNullOrEmpty(source))
            {
                var sourceBytes = Convert.FromBase64String(source);
                var data = (new SHA256Managed()).ComputeHash(Encoding.UTF8.GetBytes(BaseString));
                byte[] decryptionResult;
                using (var ms = new MemoryStream(sourceBytes))
                using (var alg = new RijndaelManaged())
                {
                    alg.IV = data.Take(alg.BlockSize / 8).ToArray();
                    alg.Key = data.Take(32).ToArray();
                    using (var crstr = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Read))
                        decryptionResult = crstr.AllBytes();
                }
                result = Encoding.UTF8.GetString(decryptionResult);
            }
            return result;
        }

        private static string encryptString(string source)
        {
            var result = source;
            if (!string.IsNullOrEmpty(result))
            {
                var sourceBytes = Encoding.UTF8.GetBytes(source);
                var data = (new SHA256Managed()).ComputeHash(Encoding.UTF8.GetBytes(BaseString));
                byte[] encryptionResult;
                using (var ms = new MemoryStream())
                using (var alg = new RijndaelManaged())
                {
                    alg.IV = data.Take(alg.BlockSize/8).ToArray();
                    alg.Key = data.Take(32).ToArray();
                    using (var crstr = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                        crstr.Write(sourceBytes, 0, sourceBytes.Length);
                    ms.Close();
                    encryptionResult = ms.ToArray();
                }
                result = Convert.ToBase64String(encryptionResult);
            }
            return result;
        }

        #endregion
    }

    /// <summary>
    /// Model extender
    /// </summary>
    public interface IModelAppExtender : IModelNode
    {
        /// <summary>
        /// Gets the model replication node.
        /// </summary>
        [Description("Replication parameters")]
        IModelReplicationNode Replication { get; }
    }

    /// <summary>
    /// Replication key search mode
    /// </summary>
    public enum ReplicationKeySearchMode
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Case insensitive search
        /// </summary>
        CaseInsensitive,
        /// <summary>
        /// Space insensitive search
        /// </summary>
        SpaceInsensitive,
        /// <summary>
        /// Case and space insensitive search
        /// </summary>
        CaseAndSpaceInsensitive
    }

    /// <summary>
    /// Replication key node
    /// </summary>
    public interface IReplicationKeyNode : IModelNode
    {
        /// <summary>
        /// Gets or sets the replication key member. 
        /// ReplicationKeyMember should uniquely identifies business object in any database. 
        /// It used in object search criteria at replication load time.
        /// </summary>
        /// <value>
        /// The replication key member.
        /// </value>
        [Description("ReplicationKeyMember should uniquely identify business object in any database."
            + " It used in object search criteria at replication package load time."
            + " Don't assign reference, key or collection members.")]
        [Category("Replication")]
        [DataSourceProperty("AllMembers")]
        IModelMember ReplicationKeyMember { get; set; }

        /// <summary>
        /// Gets or sets the replication key search mode.
        /// </summary>
        /// <value>
        /// The replication key search mode.
        /// </value>
        [Description("ReplicationKeySearchMode specifies whether XafDelta have to use" +
            " case insensitive seach while looking for object by replication key value")]
        [Category("Replication")]
        ReplicationKeySearchMode ReplicationKeySearchMode { get; set; }
    }

    /// <summary>
    /// Replication key node logic
    /// </summary>
    [DomainLogic(typeof(IReplicationKeyNode))]
    public class ReplicationKeyNodeLogic
    {
        private static readonly Dictionary<string, string> predefinedKeys = new Dictionary<string, string>{
                                                 { "DevExpress.Persistent.BaseImpl.Analysis", "Name" }, 
                                                 { "DevExpress.Persistent.BaseImpl.Country", "Name" }, 
                                                 { "DevExpress.Persistent.BaseImpl.Event", "Subject" },
                                                 { "DevExpress.Persistent.BaseImpl.HCategory", "Name" }, 
                                                 { "DevExpress.Persistent.BaseImpl.Organization", "Name" },
                                                 { "DevExpress.Persistent.BaseImpl.Person", "FullName" }, 
                                                 { "DevExpress.Persistent.BaseImpl.PhoneType", "TypeName" },
                                                 { "DevExpress.Persistent.BaseImpl.Resource", "Caption" },
                                                 { "DevExpress.Persistent.BaseImpl.RoleBase", "Name" },
                                                 { "DevExpress.Persistent.BaseImpl.State", "LongName" }, 
                                                 { "DevExpress.Persistent.BaseImpl.Task", "Subject" },
                                                 { "DevExpress.ExpressApp.Reports.ReportData", "ReportName" }};


        /// <summary>
        /// Get the replication key member.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Replication key member</returns>
        public static IModelMember Get_ReplicationKeyMember(IReplicationKeyNode node)
        {
            IModelMember result = null;
            if (node is IModelClass)
            {
                var modelClass = node as IModelClass;
                foreach (var member in modelClass.OwnMembers.Where(x => !x.MemberInfo.IsKey && !x.MemberInfo.IsList))
                {
                    if (member.MemberInfo.FindAttribute<ReplicationKeyAttribute>() != null)
                    {
                        result = member;
                        break;
                    }
                }

                if (result == null)
                {
                    string keyMemberName;
                    if(predefinedKeys.TryGetValue(modelClass.Name, out keyMemberName))
                        result = modelClass.AllMembers[keyMemberName];
                }
            }
            return result;
        }


        /// <summary>
        /// Get_s the replication key search mode.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>Replication key search mode</returns>
        public static ReplicationKeySearchMode Get_ReplicationKeySearchMode(IReplicationKeyNode node)
        {
            var result = ReplicationKeySearchMode.None;
            if (node is IModelClass)
            {
                var modelClass = node as IModelClass;
                foreach (var member in modelClass.OwnMembers.Where(x => !x.MemberInfo.IsKey && !x.MemberInfo.IsList))
                {
                    var attr = member.MemberInfo.FindAttribute<ReplicationKeyAttribute>();
                    if (attr != null)
                    {
                        if(attr.IsCaseInsensitive && attr.IsSpaceInsensitive)
                            result = ReplicationKeySearchMode.CaseAndSpaceInsensitive;
                        else if (attr.IsCaseInsensitive)
                            result = ReplicationKeySearchMode.CaseInsensitive;
                        else if (attr.IsSpaceInsensitive)
                            result = ReplicationKeySearchMode.SpaceInsensitive;
                        break;
                    }
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Model extensions helper
    /// </summary>
    public static class ExtensionsHelper
    {

        /// <summary>
        /// Extends the model interfaces.
        /// </summary>
        /// <param name="extenders">The extenders.</param>
        public static void ExtendModelInterfaces(ModelInterfaceExtenders extenders)
        {
            extenders.Add<IModelApplication, IModelAppExtender>();
            extenders.Add<IModelClass, IClassSelectorsNode>();
            extenders.Add<IModelMember, INonSnapshotMemberNode>();
            extenders.Add<IModelClass, INotForProtocolNode>();
            extenders.Add<IModelMember, INotForProtocolNode>();
            extenders.Add<IModelClass, IReplicationKeyNode>();
        }

        /// <summary>
        /// Determine whether specified <paramref name="modelClass"/> should not be included in snapshots.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>NonSnapshot attribute value</returns>
        public static bool NonSnapshot(this IModelClass modelClass)
        {
            return modelClass != null && modelClass.TypeInfo.FindAttribute<NonSnapshotAttribute>() != null;
        }

        /// <summary>
        /// Determine whether specified <paramref name="modelMember"/> should not be included in snapshots.
        /// </summary>
        /// <param name="modelMember">The model member.</param>
        /// <returns>NonSnapshot attribute value</returns>
        public static bool NonSnapshot(this IModelMember modelMember)
        {
            return modelMember == null || modelMember.GetValue<bool>("NonSnapshot");
        }


        /// <summary>
        /// Determine whether specified <paramref name="modelMember"/> changes should not be registered in protocol.
        /// </summary>
        /// <param name="modelMember">The model member.</param>
        /// <returns>NotForProtocol attribute value</returns>
        public static bool NotForProtocol(this IModelMember modelMember)
        {
            return modelMember == null || modelMember.GetValue<bool>("NotForProtocol");
        }

        /// <summary>
        /// Determine whether specified <paramref name="modelClass"/> changes should not be registered in protocol.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>NotForProtocol attribute value</returns>
        public static bool NotForProtocol(this IModelClass modelClass)
        {
            return modelClass == null || modelClass.GetValue<bool>("NotForProtocol");
        }

        /// <summary>
        /// Get ReplicationKeyIsCaseInsensitive attribute for <paramref name="modelClass"/>.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>ReplicationKeyIsCaseInsensitive attribute value</returns>
        public static bool ReplicationKeyIsCaseInsensitive(this IModelClass modelClass)
        {
            var value = modelClass.GetValue<ReplicationKeySearchMode>("ReplicationKeySearchMode");
            return value == ReplicationKeySearchMode.CaseInsensitive || value == ReplicationKeySearchMode.CaseAndSpaceInsensitive;
        }

        /// <summary>
        /// Get ReplicationKeyIsSpaceInsensitive attribute for <paramref name="modelClass"/>.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>ReplicationKeyIsSpaceInsensitive attribute value</returns>
        public static bool ReplicationKeyIsSpaceInsensitive(this IModelClass modelClass)
        {
            var value = modelClass.GetValue<ReplicationKeySearchMode>("ReplicationKeySearchMode");
            return value == ReplicationKeySearchMode.SpaceInsensitive 
                || value == ReplicationKeySearchMode.CaseAndSpaceInsensitive;
        }

        /// <summary>
        /// Get ReplicationKeyMember attribute for <paramref name="modelClass"/>.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>ReplicationKey member</returns>
        public static IModelMember ReplicationKeyMember(this IModelClass modelClass)
        {
            var result = modelClass.GetValue<IModelMember>("ReplicationKeyMember");
            return result;
        }

        /// <summary>
        /// Gets the replication key member for <paramref name="typeInfo"/>.
        /// </summary>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>Replication key member</returns>
        public static IModelMember GetReplicationKeyMember(ITypeInfo typeInfo)
        {
            IModelMember result = null;
            var model = XafDeltaModule.XafApp.Model;
            if (model != null && typeInfo != null)
            {
                var modelClas = model.BOModel.GetClass(typeInfo.Type);
                if (modelClas != null)
                {
                    result = modelClas.ReplicationKeyMember();
                    if (result == null && typeInfo.Base != null)
                        result = GetReplicationKeyMember(typeInfo.Base);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the replication key member for object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Replication key member</returns>
        public static IModelMember GetReplicationKeyMember(object source)
        {
            IModelMember result = null;
            if (source != null)
            {
                var obs = ObjectSpace.FindObjectSpaceByObject(source);
                if (obs != null)
                {
                    var ti = obs.TypesInfo.FindTypeInfo(source.GetType());
                    result = GetReplicationKeyMember(ti);
                }
                else
                {
                    var ti = XafDeltaModule.XafApp.Model.BOModel.GetClass(source.GetType()).TypeInfo;
                    result = GetReplicationKeyMember(ti);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the replication key value for object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Replication key value</returns>
        public static string GetReplicationKey(object source)
        {
            object result = null;
            var keyMember = GetReplicationKeyMember(source);
            if (keyMember != null)
                result = keyMember.MemberInfo.GetValue(source);
            return result == null ? null : result.ToString();
        }

        /// <summary>
        /// Get RecipientSelectors node for model class.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>RecipientSelectors node</returns>
        public static IRecipientSelectorNodeList RecipientSelectors(this IModelClass modelClass)
        {
            /* 11.2.7 */
            return (IRecipientSelectorNodeList)modelClass.GetNode("RecipientSelectors");
        }

        /// <summary>
        /// Gets all recipient selectors for <paramref name="modelClass"/>.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>All recipient selectors for <paramref name="modelClass"/></returns>
        public static IEnumerable<IRecipientSelectorNode> AllRecipientSelectors(this IModelClass modelClass)
        {
            IEnumerable<IRecipientSelectorNode> result = new List<IRecipientSelectorNode>();
            if (modelClass != null)
                result = from c in modelClass.AllParents() from s in c.RecipientSelectors().OrderBy(x => x.Index) select s;
            return result;
        }

        /// <summary>
        /// Get SnapshotSelectors for <paramref name="modelClass"/>.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>SnapshotSelectors for model class</returns>
        public static ISnapshotSelectorNodeList SnapshotSelectors(this IModelClass modelClass)
        {
            return (ISnapshotSelectorNodeList)modelClass.GetNode("SnapshotSelectors");
        }

        /// <summary>
        /// Gets all snapshot selectors for <paramref name="modelClass"/>.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>Gets all snapshot selectors for <paramref name="modelClass"/></returns>
        public static IEnumerable<ISnapshotSelectorNode> AllSnapshotSelectors(this IModelClass modelClass)
        {
            IEnumerable<ISnapshotSelectorNode> result = new List<ISnapshotSelectorNode>();
            if(modelClass != null)
                result = from c in modelClass.AllParents() from s in c.SnapshotSelectors().OrderBy(x => x.Index) select s;
            return result;
        }

        /// <summary>
        /// Gets all parents classes for specified <paramref name="modelClass"/> (include itself).
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <returns>All parents classes for specified <paramref name="modelClass"/> (include itself)</returns>
        public static IEnumerable<IModelClass> AllParents(this IModelClass modelClass)
        {
            var result = new List<IModelClass>();
            if(modelClass != null)
            {
                result.AddRange(AllParents(modelClass.BaseClass));
                result.Add(modelClass);
            }
            return result.Distinct();
        }
    }

    // ReSharper restore InconsistentNaming
}
