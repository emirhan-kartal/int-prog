function openModifyFileModal(fileId, currentName) {
    document.getElementById('fileId').value = fileId;
    document.querySelector('#modifyFileModal input[name="newName"]').value = currentName;
    new bootstrap.Modal(document.getElementById('modifyFileModal')).show();
} 