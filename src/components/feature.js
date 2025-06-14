import React from 'react'

import PropTypes from 'prop-types'

import './feature.css'

const Feature = (props) => {
  return (
    <div className={`feature-feature ${props.rootClassName} `}>
      <img
        alt={props.thumbnailAlt}
        src={props.thumbnail}
        className="feature-image"
      />
      <div className="feature-content">
        <span className="feature-title">{props.title}</span>
        <span className="feature-description">{props.description}</span>
      </div>
    </div>
  )
}

Feature.defaultProps = {
  title: 'Account',
  thumbnailAlt: 'image',
  thumbnail: '/vector.svg',
  rootClassName: '',
  description:
    'Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusa',
}

Feature.propTypes = {
  title: PropTypes.string,
  thumbnailAlt: PropTypes.string,
  thumbnail: PropTypes.string,
  rootClassName: PropTypes.string,
  description: PropTypes.string,
}

export default Feature
