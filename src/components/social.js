import React from 'react'

import PropTypes from 'prop-types'

import './social.css'

const Social = (props) => {
  return (
    <div className={`social-social ${props.rootClassName} `}>
      <img
        alt={props.insiderAlt}
        src={props.insiderSrc}
        className="social-insider"
      />
    </div>
  )
}

Social.defaultProps = {
  insiderAlt: 'pastedImage',
  insiderSrc: '/pastedimage-9nha.svg',
  rootClassName: '',
}

Social.propTypes = {
  insiderAlt: PropTypes.string,
  insiderSrc: PropTypes.string,
  rootClassName: PropTypes.string,
}

export default Social
