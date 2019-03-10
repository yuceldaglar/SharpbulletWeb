
function api(service) {
    return "/api.ashx?api=" + service;
}

$(document).ajaxError(function (event, jqxhr, settings, thrownError) {

    var result = null;
    if (jqxhr.responseJSON) {
        result = jqxhr.responseJSON;
    } else if (!jqxhr.responseJSON && jqxhr.responseText) {
        result = JSON.parse(jqxhr.responseText);
    }
    if (result && result.ErrorId == "notauthorized") {
        sbAlert(result.ErrorMessage);
    } else if (result && result.ErrorMessage) {
        sbAlert("Error", result.ErrorMessage);
    } else {
        sbAlert("Error", "An unknown error occured.");
    }
});

function sbAlert(title, message) {
    eModal.alert(message, title);
}