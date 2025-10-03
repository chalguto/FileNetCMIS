// <copyright file="FileNetCMISService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Core.Documento
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using BusinessExceptions;
    using DotCMIS;
    using DotCMIS.Client;
    using DotCMIS.Client.Impl;
    using DotCMIS.Data;
    using DotCMIS.Data.Impl;

    public class ConnectionFilenet
    {
        public string RepositoryId { get; set; }
        public string ServiceUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Provides methods to interact with the FileNet CMIS repository.
    /// </summary>
    public class FileNetCMISService
    {
        private string cmisUrl;
        private string username;
        private string password;
        private string repositoryId;

        /// <summary>
        /// Establishes a connection to the FileNet repository using the provided connection parameters.
        /// </summary>
        /// <param name="connectionFilenet">Connection parameters containing repository credentials and settings.</param>
        public void Connection(ConnectionFilenet connectionFilenet)
        {
            this.cmisUrl = connectionFilenet.ServiceUrl ?? throw new ArgumentNullException(nameof(connectionFilenet.ServiceUrl));
            this.username = connectionFilenet.Username ?? throw new ArgumentNullException(nameof(connectionFilenet.Username));
            this.password = connectionFilenet.Password ?? throw new ArgumentNullException(nameof(connectionFilenet.Password));
            this.repositoryId = connectionFilenet.RepositoryId ?? throw new ArgumentNullException(nameof(connectionFilenet.RepositoryId));
        }

            /// <summary>
    /// Uploads a document to a specified folder in a CMIS-compliant repository after validating its uniqueness.
    /// The method converts base64-encoded content into a file and stores it with the given name.
    /// </summary>
    /// <param name="folderPath">The path of the target folder where the document will be uploaded.</param>
    /// <param name="documentName">The name of the document (including file extension) to be created.</param>
    /// <param name="base64Content">The document content encoded as a base64 string.</param>
    /// <returns>
    /// A dictionary containing:
    /// - Key "DocumentName": The validated document name (ensuring no duplicates).
    /// - Key "DocumentID": The unique identifier assigned to the document in the CMIS repository.
    /// Returns null if the operation fails (though not explicitly shown in this implementation).
    /// </returns>
    public Dictionary<string, string> UploadDocument(string folderPath, string documentName, string base64Content)
    {
        IFolder targetFolder = this.GetTargetFolder(this.CreateCmisSession(), folderPath);
        string valDocumentName = this.ValidateDocumentDoesNotExist(targetFolder, documentName);
        string documentID = this.CreateDocumentInFolder(targetFolder, valDocumentName, this.ConvertFromBase64(base64Content));

        return new Dictionary<string, string>
        {
            { "DocumentName", valDocumentName },
            { "DocumentID", documentID },
        };
    }

    /// <summary>
    /// Retrieves a document from the repository using its unique identifier.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document to retrieve.</param>
    /// <returns>The document object if found.</returns>
    public IDocument GetDocumentById(string documentId)
    {
        try
        {
            ISession session = null;

            session = this.CreateCmisSession();

            // Intentar obtener el documento directamente por ID
            ICmisObject cmisObject = session.GetObject(documentId);

            if (cmisObject is IDocument document)
            {
                return document;
            }

            throw new ExceptionFilenetCMIS($"El objeto con ID {documentId} no es un documento");
        }
        catch (DotCMIS.Exceptions.CmisObjectNotFoundException)
        {
            throw new ExceptionFilenetCMIS($"No se encontró ningún documento con el ID {documentId}");
        }
        catch (Exception ex)
        {
            throw new ExceptionFilenetCMIS($"Error al buscar el documento con ID {documentId}", ex);
        }
    }

    /// <summary>
    /// Elimina un documento en FileNet utilizando su identificador único (ID) a través de una sesión CMIS.
    /// </summary>
    /// <param name="documentId">Identificador único del documento en FileNet que se desea eliminar.</param>
    /// <exception cref="ExceptionFilenetCMIS">
    /// Se lanza si ocurre un error durante la eliminación del documento en FileNet.
    /// </exception>
    public void DeleteDocumentById(string documentId)
    {
        try
        {
            ISession session = this.CreateCmisSession();
            IObjectId objectId = session.GetObject(documentId);
            session.Delete(objectId, true);
        }
        catch (Exception)
        {
            throw new ExceptionFilenetCMIS($"No se pudo eliminar el objeto con ID {documentId}");
        }
    }

    /// <summary>
    /// Creates a CMIS session using the provided connection details.
    /// </summary>
    /// <returns>An active CMIS session.</returns>
    public ISession CreateCmisSession()
    {
        var parameters = new Dictionary<string, string>
        {
            [SessionParameter.BindingType] = BindingType.AtomPub,
            [SessionParameter.AtomPubUrl] = this.cmisUrl,
            [SessionParameter.User] = this.username,
            [SessionParameter.Password] = this.password,
            [SessionParameter.RepositoryId] = this.repositoryId,
        };

        try
        {
            return SessionFactory.NewInstance().CreateSession(parameters);
        }
        catch (Exception ex)
        {
            throw new ExceptionFilenetCMIS("Error al conectarse al repositorio", ex);
        }
    }

    /// <summary>
    /// Retrieves the path of the first parent folder of a document identified by the given CMIS object ID.
    /// </summary>
    /// <param name="id">The CMIS object ID of the document.</param>
    /// <returns>The first path of the document's parent folder.</returns>
    public string GetPathById(string id)
    {
        ISession session = this.CreateCmisSession();
        IDocument document = session.GetObject(id) as IDocument;

        IList<IFolder> parents = document.Parents;
        IFolder parent = parents[0];

        return parent.Paths[0];
    }

    /// <summary>
    /// Retrieves a list of documents located in the folder specified by the given path.
    /// </summary>
    /// <param name="folderPath">The CMIS path of the folder from which to retrieve documents.</param>
    /// <returns>A list of documents found in the specified folder.</returns>
    /// <exception cref="ArgumentException">Thrown when the folder path is null, empty, or whitespace.</exception>
    /// <exception cref="ApplicationException">Thrown when an error occurs while retrieving documents from the folder.</exception>
    public IList<IDocument> GetDocumentsByPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("La ruta de la carpeta no puede estar vacía", nameof(folderPath));
        }

        var session = this.CreateCmisSession();

        try
        {
            IFolder targetFolder = this.GetFolderByPath(session, folderPath);
            return this.GetDocumentsFromFolder(targetFolder);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener documentos de la ruta '{folderPath}'", ex);
        }
    }

    /// <summary>
    /// Retrieves a folder from the CMIS repository using the specified path.
    /// If the path is root or empty, the root folder is returned.
    /// </summary>
    /// <param name="session">The CMIS session used to access the repository.</param>
    /// <param name="folderPath">The path of the folder to retrieve.</param>
    /// <returns>The folder object corresponding to the specified path.</returns>
    /// <exception cref="ApplicationException">
    /// Thrown when the folder does not exist or an error occurs while accessing the path.
    /// </exception>
    private IFolder GetFolderByPath(ISession session, string folderPath)
    {
        try
        {
            // Normalizar la ruta
            folderPath = folderPath.Trim();

            // Si la ruta es la raíz o está vacía
            if (folderPath == "/" || string.IsNullOrEmpty(folderPath.Trim('/', '\\')))
            {
                return session.GetRootFolder();
            }

            // Asegurar que la ruta comience con /
            if (!folderPath.StartsWith("/"))
            {
                folderPath = "/" + folderPath;
            }

            // Obtener la carpeta por path
            if (!(session.GetObjectByPath(folderPath) is IFolder folder))
            {
                throw new ApplicationException($"No se encontró la carpeta en la ruta: {folderPath}");
            }

            return folder;
        }
        catch (DotCMIS.Exceptions.CmisObjectNotFoundException)
        {
            throw new ApplicationException($"La carpeta no existe en la ruta: {folderPath}");
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al acceder a la ruta: {folderPath}", ex);
        }
    }

    /// <summary>
    /// Obtiene todos los documentos contenidos directamente en una carpeta del repositorio CMIS.
    /// </summary>
    /// <param name="folder">Carpeta desde la cual se desean obtener los documentos.</param>
    private IList<IDocument> GetDocumentsFromFolder(IFolder folder)
    {
        var documents = new List<IDocument>();

        foreach (var child in folder.GetChildren())
        {
            if (child is IDocument document)
            {
                documents.Add(document);
            }
        }

        return documents;
    }

    /// <summary>
    /// Converts a Base64 string to a byte array.
    /// </summary>
    /// <param name="base64Content">The Base64 string to convert.</param>
    /// <returns>The converted byte array.</returns>
    private byte[] ConvertFromBase64(string base64Content)
    {
        try
        {
            return Convert.FromBase64String(base64Content);
        }
        catch (FormatException ex)
        {
            throw new ExceptionFilenetCMIS($"El contenido proporcionado no es un Base64 válido. ConvertFromBase64. base64Content: {nameof(base64Content)}", ex);
        }
    }

    /// <summary>
    /// Retrieves the target folder from the repository based on the specified path.
    /// </summary>
    /// <param name="session">The CMIS session.</param>
    /// <param name="folderPath">The path of the folder to retrieve.</param>
    /// <returns>The target folder.</returns>
    private IFolder GetTargetFolder(ISession session, string folderPath)
    {
        return this.GetOrCreateFolderStructure(session, session.GetRootFolder(), folderPath);
    }

    /// <summary>
    /// Validates that a document with the specified name does not already exist in the folder.
    /// </summary>
    /// <param name="folder">The folder to check.</param>
    /// <param name="documentName">The name of the document to validate.</param>
    /// <exception cref="ExceptionFilenetCMIS">Thrown if the document already exists.</exception>
    private string ValidateDocumentDoesNotExist(IFolder folder, string documentName)
    {
        if (this.DocumentExists(folder, documentName))
        {
            return this.GenerateUniquePrefixedName(documentName);
        }

        return documentName;
    }

    /// <summary>
    /// Generates a unique name by combining a timestamp, GUID portion, and the original document name.
    /// </summary>
    /// <param name="documentName">The original name of the document that will be included in the result.</param>
    /// <returns>
    /// A string containing:
    /// 1. A 7-digit hexadecimal representation of the current timestamp's least significant bits
    /// 2. A 4-character segment from a new GUID
    /// 3. The original document name
    /// All components are joined with underscore separators.
    /// </returns>
    private string GenerateUniquePrefixedName(string documentName)
    {
        long ticks = DateTime.UtcNow.Ticks;
        string timestampPart = (ticks & 0xFFFFFFF).ToString("X7");

        string guidPart = Guid.NewGuid().ToString("N").Substring(0, 4);

        return $"{timestampPart}_{guidPart}_{documentName}";
    }

    /// <summary>
    /// Creates a document in the specified folder.
    /// </summary>
    /// <param name="folder">The folder where the document will be created.</param>
    /// <param name="documentName">The name of the document to create.</param>
    /// <param name="content">The content of the document as a byte array.</param>
    /// <returns>The ID of the created document.</returns>
    private string CreateDocumentInFolder(IFolder folder, string documentName, byte[] content)
    {
        try
        {
            var documentProps = new Dictionary<string, object>
            {
                [PropertyIds.Name] = documentName,
                [PropertyIds.ObjectTypeId] = "cmis:document",
            };

            IContentStream contentStream = new ContentStream
            {
                FileName = documentName,
                MimeType = this.GetMimeType(documentName),
                Length = content.Length,
                Stream = new MemoryStream(content),
            };

            IDocument newDocument = folder.CreateDocument(documentProps, contentStream, null);
            return newDocument.Id;
        }
        catch (Exception ex)
        {
            throw new ExceptionFilenetCMIS($"Error guardando el documento '{documentName}'. CreateDocumentInFolder", ex);
        }
    }

    /// <summary>
    /// Determines the MIME type of a file based on its extension.
    /// </summary>
    /// <param name="filename">The name of the file whose MIME type is to be determined.</param>
    /// <returns>The MIME type as a string. Defaults to "application/octet-stream" if the extension is not recognized.</returns>
    private string GetMimeType(string filename)
    {
        string extension = Path.GetExtension(filename)?.ToLowerInvariant();

        var mimeTypes = new Dictionary<string, string>
        {
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".zip", "application/zip" },
            { ".json", "application/json" },
            { ".xml", "application/xml" },
        };

        return mimeTypes.TryGetValue(extension, out string mimeType) ? mimeType : "application/octet-stream";
    }

    /// <summary>
    /// Retrieves or creates the folder structure specified by the folder path.
    /// </summary>
    /// <param name="session">The CMIS session.</param>
    /// <param name="rootFolder">The root folder of the repository.</param>
    /// <param name="folderPath">The folder path to retrieve or create.</param>
    /// <returns>The target folder.</returns>
    /// <exception cref="ApplicationException">Thrown when an error occurs while creating a folder.</exception>
    private IFolder GetOrCreateFolderStructure(ISession session, IFolder rootFolder, string folderPath)
    {
        IFolder currentFolder = rootFolder;
        string[] folderNames = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string folderName in folderNames)
        {
            IFolder subFolder = this.GetChildFolderByName(session, currentFolder, folderName);

            if (subFolder == null)
            {
                try
                {
                    var newFolderProps = new Dictionary<string, object>
                    {
                        [PropertyIds.Name] = folderName,
                        [PropertyIds.ObjectTypeId] = "cmis:folder",
                    };

                    subFolder = currentFolder.CreateFolder(newFolderProps);
                }
                catch (Exception ex)
                {
                    throw new ExceptionFilenetCMIS($"Error creando la subcarpeta '{folderName}'. GetOrCreateFolderStructure", ex);
                }
            }

            currentFolder = subFolder;
        }

        return currentFolder;
    }

    /// <summary>
    /// Retrieves a child folder by its name from the specified parent folder.
    /// </summary>
    /// <param name="session">The CMIS session.</param>
    /// <param name="parentFolder">The parent folder.</param>
    /// <param name="folderName">The name of the child folder to retrieve.</param>
    /// <returns>The child folder if found; otherwise, null.</returns>
    private IFolder GetChildFolderByName(ISession session, IFolder parentFolder, string folderName)
    {
        string query = $"" +
            $"SELECT cmis:objectId " +
            $"FROM cmis:folder " +
            $"WHERE cmis:name = '{folderName}' " +
            $"AND IN_FOLDER('{parentFolder.Id}')";

        IItemEnumerable<IQueryResult> results = session.Query(query, false);

        foreach (var result in results)
        {
            string objectId = result["cmis:objectId"].FirstValue.ToString();
            return (IFolder)session.GetObject(objectId);
        }

        return null;
    }

    /// <summary>
    /// Checks if a document with the specified name exists in the given folder.
    /// </summary>
    /// <param name="folder">The folder to check.</param>
    /// <param name="documentName">The name of the document to check for.</param>
    /// <returns>True if the document exists; otherwise, false.</returns>
    private bool DocumentExists(IFolder folder, string documentName)
    {
        foreach (var child in folder.GetChildren())
        {
            if (child is IDocument document && string.Equals(document.Name, documentName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
  }
}
