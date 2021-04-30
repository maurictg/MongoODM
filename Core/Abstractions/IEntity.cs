namespace Core.Abstractions
{
    public interface IEntity
    {
        /// <summary>
        /// Deep clone the entity instance
        /// </summary>
        /// <returns>The cloned instance</returns>
        public IEntity Clone();
    }
}