// <copyright file="download.js" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

/**
 * Downloads a file from provided data
 * @param {string} filename - The name of the file to download
 * @param {string} contentType - The MIME type of the file
 * @param {string} data - The file content (for CSV, this is the text data)
 */
window.downloadFile = function (filename, contentType, data) {
  // Create a Blob from the data
  const blob = new Blob([data], {type: contentType});

  // Create a temporary URL for the blob
  const url = window.URL.createObjectURL(blob);

  // Create a temporary anchor element
  const anchorElement = document.createElement('a');
  anchorElement.href = url;
  anchorElement.download = filename;

  // Trigger the download
  document.body.appendChild(anchorElement);
  anchorElement.click();

  // Clean up
  document.body.removeChild(anchorElement);
  window.URL.revokeObjectURL(url);
};
