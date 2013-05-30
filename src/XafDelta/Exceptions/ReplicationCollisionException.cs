using System;
using XafDelta.Localization;
using XafDelta.Replication;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Replication collision exception
    /// </summary>
    [Serializable]
    public class ReplicationCollisionException : ApplicationException
    {
        /// <summary>
        /// Gets the type of the collision.
        /// </summary>
        /// <value>
        /// The type of the collision.
        /// </value>
        public CollisionType CollisionType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicationCollisionException"/> class.
        /// </summary>
        /// <param name="collisionType">Type of the collision.</param>
        public ReplicationCollisionException(CollisionType collisionType) :
            base(getCollisionMessage(collisionType))
        {
            CollisionType = collisionType;
        }

        private static string getCollisionMessage(CollisionType collisionType)
        {
            var result = string.Format(Localizer.CollisionError, collisionType);
            switch (collisionType)
            {
                case CollisionType.TargetObjectAlreadyExists:
                    result += Localizer.TargetObjectExists;
                    break;
                case CollisionType.OldObjectIsNotFound:
                    break;
                case CollisionType.NewObjectIsNotFound:
                    break;
                case CollisionType.TargetObjectIsNotFound:
                    result += Localizer.TargetObjectNotFound;
                    break;
            }
            return result;
        }
    }
}