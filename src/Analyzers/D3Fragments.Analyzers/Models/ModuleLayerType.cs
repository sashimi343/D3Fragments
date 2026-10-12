namespace D3Fragments.Analyzers.Models;

internal enum ModuleLayerType
{
    Other,
    Presentation_Other,
    Presentation_Controller,

    Application_Other,
    Application_Request,
    Application_RequestHandler,
    Application_RequestExecutor,
    Application_DomainEventHandler,
    Application_PipelineBehavior,

    Domain_Other,
    Domain_Entity,
    Domain_ValueObject,
    Domain_RepositoryInterface,
    Domain_DomainRule,

    Infrastructure_Other,
    Infrastructure_RepositoryImplementation,
    Infrastructure_DbContext,
    Infrastructure_UnitOfWork,
}
