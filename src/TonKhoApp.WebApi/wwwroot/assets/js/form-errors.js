
var FIELD_ERROR_CSS = {
    display: 'block',
    color: '#c0392b',
    fontSize: '12px',
    lineHeight: '1.4',
    textAlign: 'left',
    margin: '4px 0 10px'
};

function clearOneFieldError(input) {
    var $input = $(input);
    $input.next('.field-error').remove();
    if (!$input.hasClass('field-invalid')) return;
    $input.removeClass('field-invalid')
          .css({ borderColor: '', marginBottom: $input.data('mb-old') || '' })
          .removeData('mb-old');
}

function clearFieldErrors(scope) {
    var $scope = $(scope || document);
    $scope.find('.field-error').remove();
    $scope.find('.field-invalid').each(function () { clearOneFieldError(this); });
}

function bindFieldErrorReset(scope) {
    $(scope || document).on('input change', 'input, select, textarea', function () {
        clearOneFieldError(this);
    });
}

function setFieldError(selector, message) {
    var $input = $(selector);
    if (!$input.length || !message) return false;

    if (!$input.hasClass('field-invalid')) {
        $input.data('mb-old', $input.css('margin-bottom'))
              .addClass('field-invalid')
              .css({ borderColor: '#c0392b', marginBottom: '2px' });
    }

    $input.next('.field-error').remove();
    $('<div>').addClass('field-error').css(FIELD_ERROR_CSS).text(message).insertAfter($input);
    return true;
}

function showFieldErrors(result, fieldMap) {
    if (!result || !result.errors) return false;

    var lookup = {};
    Object.keys(fieldMap).forEach(function (k) {
        lookup[k.toLowerCase()] = fieldMap[k];
    });

    var placed = 0;
    Object.keys(result.errors).forEach(function (key) {
        var selector = lookup[String(key).toLowerCase()];
        var messages = [].concat(result.errors[key]);
        if (selector && setFieldError(selector, messages[0])) placed++;
    });

    if (placed) $('.field-invalid').first().trigger('focus');
    return placed > 0;
}
