using System.Xml.Linq;

namespace Izio.Umbraco.ContentUtilities.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreator
    {
        /// <summary>
        /// 
        /// </summary>
        void Deploy(XDocument configuration);

        /// <summary>
        /// 
        /// </summary>
        void Retract(XDocument configuration);
    }
}