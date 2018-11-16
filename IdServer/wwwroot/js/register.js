
$(document).ready(function () {
  
  var userName = $("#Username");
  var password = $("#Password");
  var passwordR = $("#PasswordRepeated");
  var register = $("#Register");
  var input = $("input");
  
  function canRegister()
  {
    return userName.val().length > 0 && password.val().length > 0 && password.val() == passwordR.val();
  }

  function setEnabled()
  {
    if(canRegister())
    {
      register.removeAttr("disabled");
    }
    else
    {
      register.attr("disabled", "disabled");
    }
  }

  setEnabled();

  input.keyup(function(){
    setEnabled();
  })

});
