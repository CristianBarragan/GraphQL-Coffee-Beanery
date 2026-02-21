// namespace Domain.Shared.Mapping;
//
// public class ContractMap
// {
//     public void Register()
//     {
//         var contactPoint = new NodeMap
//         {
//             IsModel = true
//         };
//         
//         MappingRegistry.Register(nameof(ContactPoint), contactPoint);
//     }
//     
//     public string MappingNode { get; set; } = nameof(ContactPoint);
//     
//     public static void MapTo(ContactPoint contactPoint, DataEntity.ContactPoint contactPointMapping)
//     {
//         contactPoint ??= new ContactPoint();
//         contactPoint.ContactPointKey = contactPointMapping.ContactPointKey;
//         contactPoint.ContactPointType = (ContactPointType)contactPointMapping.ContactPointType;
//         contactPoint.ContactPointValue = contactPointMapping.ContactPointValue;
//     }
//     
//     
// }