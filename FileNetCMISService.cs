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
        /// Uploads a document to the specified folder in the repository.
        /// </summary>
        /// <param name="folderPath">The path of the folder where the document will be uploaded.</param>
        /// <param name="documentName">The name of the document to upload.</param>
        /// <param name="base64Content">The content of the document in Base64 format.</param>
        /// <returns>The ID of the uploaded document.</returns>
        public string UploadDocument(string folderPath, string documentName, string base64Content)
        {
            byte[] documentContent = this.ConvertFromBase64(base64Content);

            ISession session = null;

            session = this.CreateCmisSession();
            IFolder targetFolder = this.GetTargetFolder(session, folderPath);
            this.ValidateDocumentDoesNotExist(targetFolder, documentName);

            return this.CreateDocumentInFolder(targetFolder, documentName, documentContent);
        }

        /// <summary>
        /// Creates a CMIS session using the provided connection details.
        /// </summary>
        /// <returns>An active CMIS session.</returns>
        private ISession CreateCmisSession()
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
        private void ValidateDocumentDoesNotExist(IFolder folder, string documentName)
        {
            if (this.DocumentExists(folder, documentName))
            {
                throw new ExceptionFilenetCMIS($"El documento '{documentName}' ya existe en la carpeta.");
            }
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
        private IFolder GetOrCreateFolderStructure(DotCMIS.Client.ISession session, IFolder rootFolder, string folderPath)
        {
            IFolder currentFolder = rootFolder;
            string[] folderNames = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string folderName in folderNames)
            {
                IFolder subFolder = this.GetChildFolderByName(currentFolder, folderName);

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
        /// <param name="parentFolder">The parent folder.</param>
        /// <param name="folderName">The name of the child folder to retrieve.</param>
        /// <returns>The child folder if found; otherwise, null.</returns>
        private IFolder GetChildFolderByName(IFolder parentFolder, string folderName)
        {
            foreach (var child in parentFolder.GetChildren())
            {
                if (child is IFolder folder && string.Equals(folder.Name, folderName, StringComparison.OrdinalIgnoreCase))
                {
                    return folder;
                }
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

        /// <summary>
        /// Retrieves a document from the repository using its unique identifier.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document to retrieve.</param>
        /// <returns>The document object if found.</returns>
        /// <exception cref="ExceptionFilenetCMIS">
        /// Thrown when the document is not found, the object is not a document, or an error occurs during retrieval.
        /// </exception>
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
    }
}